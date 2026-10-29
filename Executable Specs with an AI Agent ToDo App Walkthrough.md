# Executable Specs with an AI Agent: ToDo App Walkthrough

Oct 22, 2026 · @Daniel Huxham

## What you'll build

You'll build a tiny ToDo app (Angular front end, C# REST API) where you never write the app code yourself. You write executable specifications, lock them, and let Claude Code make them pass. Plan on two or three evenings.

By the end you'll have practised the full loop four times, including one unhappy path, and you'll have proved the payoff: swapping the storage layer without touching a single spec.

**Who owns what**

You own everything and review everything. The agent works in one of three roles at a time, and each role may change only its own part of the repo:

| Folder | What it holds | Agent role that may change it |
| --- | --- | --- |
| `acceptance/Specs` | The executable specifications | Spec agent |
| `acceptance/Dsl` | The domain vocabulary the specs speak | Spec agent |
| `acceptance/Drivers` | The contract: API routes and JSON, UI test IDs | Spec agent |
| `api/`, `api.Tests/`, `web/` | The app and its unit tests | App agent |
| `acceptance/Infrastructure`, `scripts/`, `.claude/`, `CLAUDE.md`, deployment files, this guide | The harness, guardrails and deployable system | Harness agent |

A fourth role, the free agent, exists for cross-cutting work. Step 5 sets all this up.

The drivers are where you make design decisions. They state which endpoints exist and which `data-testid` attributes the UI must expose, so the agent builds to your contract rather than inventing one.

## Step 1: Check your tools

You already have .NET 10, the Angular CLI, Docker and Claude Code. The one extra is PowerShell, which Playwright for .NET uses to install its browsers.

```bash
dotnet --version        # 10.x
ng version              # Angular CLI
docker compose version
claude --version

# Only if `pwsh --version` fails:
winget install --id Microsoft.PowerShell --source winget
```

## Step 2: Scaffold the repo

Three projects in one repo: the API, the Angular app and the acceptance tests. The app projects are bare templates; the agent fills them in.

```bash
mkdir todo-exec-spec && cd todo-exec-spec
git init
dotnet new gitignore

# API: the stock template, the agent will replace the weather endpoint
dotnet new webapi -n Todo.Api -o api

# Angular app: no SSR, no nested git repo
npx @angular/cli@latest new web --skip-git --ssr=false --style=css --routing=false

# Acceptance tests: xUnit v3 plus Playwright
dotnet new xunit -n Todo.AcceptanceTests -o acceptance
cd acceptance
dotnet remove package xunit
dotnet add package xunit.v3
dotnet add package Microsoft.Playwright
cd ..
```

The `xunit` template still uses xUnit v2. The plumbing needs v3, for the `Channels` attribute's runtime skipping, which is why the package is swapped. v3 test projects are executables, so add `<OutputType>Exe</OutputType>` to the `PropertyGroup` in `acceptance/Todo.AcceptanceTests.csproj`. Then delete the template's `UnitTest1.cs` and install the browser:

```bash
dotnet build acceptance
pwsh acceptance/bin/Debug/net10.0/playwright.ps1 install chromium
```

Commit: `git add -A && git commit -m "Scaffold"`.

If `ng new` asks extra questions (AI tooling, zoneless), the defaults are fine.

## Step 3: Make it deployable

Specs run against the system as it would really be deployed, so set that up before any feature exists. nginx serves the Angular build and forwards `/api` to the API container. Both drivers then use a single address: `http://localhost:4200`.

**`api/Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Todo.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Todo.Api.dll"]
```

**`web/Dockerfile`**

```dockerfile
FROM node:24-alpine AS build
WORKDIR /src
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginx:stable-alpine-slim
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/web/browser /usr/share/nginx/html
```

**`web/nginx.conf`**

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;

  location /api/ { proxy_pass http://api:8080; }
  location /     { try_files $uri $uri/ /index.html; }
}
```

**`docker-compose.yml`** (repo root)

```yaml
name: todo-exec-spec

services:
  api:
    build: ./api
    image: todo-exec-spec-api
    container_name: todo-exec-spec-api
  web:
    build: ./web
    image: todo-exec-spec-web
    container_name: todo-exec-spec-web
    ports: ["4200:80"]
    depends_on: [api]
```

Add a `.dockerignore` in `api/` containing `bin/` and `obj/`, and one in `web/` containing `node_modules/`, `dist/` and `.angular/`.

Check it works: `docker compose up -d --build`, then open `http://localhost:4200`. You should see the Angular welcome page. Commit.

## Step 4: Build the test plumbing

This is the one-off setup cost. Type these files in yourself rather than pasting, because the point is to understand each layer. Namespaces are included: the channel types are split into partial parts across folders, and every part of a partial type must share one namespace.

```
acceptance/
  Infrastructure/
    Channel.cs                  which channel this run uses
    ChannelsAttribute.cs        limits a spec to some channels
    AcceptanceTestBase.cs       builds the channel and the DSL
  Dsl/
    Alias.cs                    unique names per test
    SpecContext.cs              what a spec shares across domains
    DslBase.cs                  the base for each domain's DSL
    Todos/                      one folder per domain, like the drivers
      TodosDsl.cs               the todo vocabulary specs use
  Drivers/
    DriverResult.cs             what a driver action did
    Channels/                   one class per way of reaching the system
      IChannel.cs
      ApiChannel.cs
      UiChannel.cs
    Todos/                      one folder per domain
      ITodoDriver.cs            the domain's contract, at its root
      ITodoTextInputDriver.cs
      IChannel.Todos.cs
      Api/
        ApiTodoDriver.cs
        ApiChannel.Todos.cs
      Ui/
        UiTodoDriver.cs
        UiTodoTextInputDriver.cs
        UiChannel.Todos.cs
  Specs/                        empty for now
```

Each folder level splits on one thing. Under `Drivers/`, folders are domains (`Todos/`, later perhaps `Lists/`), and inside a domain, folders are channels. `Dsl/` is split by the same domains, so each domain's vocabulary sits beside nothing but its own. `Channels/` holds everything about a channel that isn't specific to one domain.

A **channel** is one way of reaching the whole system: the public API or the browser. It is made of small **drivers**, one per capability. A capability every channel must support, like `ITodoDriver`, is required. One only some channels support, like `ITodoTextInputDriver` (live feedback while typing, which only a browser has), is optional: channels that don't support it simply don't mention it.

### Infrastructure/Channel.cs

The `ACCEPTANCE_CHANNEL` environment variable chooses the channel, so every spec runs unchanged through the API or the browser.

```csharp
namespace acceptance.Infrastructure;

public enum Channel { Api, Ui }

// The channel this run drives the system through, from ACCEPTANCE_CHANNEL.
public static class AcceptanceChannel
{
    public static Channel Current
    {
        get
        {
            var value = Environment
                .GetEnvironmentVariable("ACCEPTANCE_CHANNEL")
                ?.Trim().ToLowerInvariant();
            return value switch
            {
                "api" => Channel.Api,
                "ui" => Channel.Ui,
                null or "" => throw new InvalidOperationException(
                    "Set ACCEPTANCE_CHANNEL to 'api' or 'ui' before running the acceptance tests."),
                _ => throw new InvalidOperationException(
                    $"Unknown ACCEPTANCE_CHANNEL '{value}'. Use 'api' or 'ui'.")
            };
        }
    }
}
```

### Infrastructure/ChannelsAttribute.cs

Most specs apply to every channel. A spec for behaviour only some channels have, such as a warning shown while typing, is marked `[Fact, Channels(Channel.Ui)]`. On other channels it is reported as skipped, so nothing disappears silently. This relies on xUnit v3's runtime skipping.

```csharp
using System.Reflection;
using Xunit.v3;

namespace acceptance.Infrastructure;

// Limits a spec to the channels it applies to: [Fact, Channels(Channel.Ui)].
// Specs without it run on every channel, which should stay the default.
// On other channels the spec is reported as skipped, never silently dropped.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ChannelsAttribute(params Channel[] channels) : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var current = AcceptanceChannel.Current;
        if (!channels.Contains(current))
            Assert.Skip($"Not applicable to the {current} channel");
    }
}
```

### Dsl/Alias.cs

Every test gets its own suffix, so "Buy milk" becomes "Buy milk_3f9a1c". Tests never see each other's data, so there's no database reset and they can run in parallel, even against a system already full of data. Blank names stay blank, so you can still test empty input. `SuffixLength` lets the DSL build titles of an exact length, alias included.

```csharp
namespace acceptance.Dsl;

public class Alias
{
    public static readonly int SuffixLength = 7;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..(SuffixLength-1)];

    public string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name}_{_suffix}";
}
```

### Drivers/DriverResult.cs

Drivers report what happened instead of throwing. The DSL decides whether that outcome was expected. The `TryCatchAsync` helpers turn an exception a driver expects, such as a Playwright timeout, into a failed result.

```csharp
namespace acceptance.Drivers;

public record DriverResult(bool IsFailed, string Error = "")
{
    public static readonly DriverResult Okay = new(false);
    public static DriverResult Fail(string error) =>
        new(true, error);

    public void ShouldSucceed(string because) =>
        Assert.False(IsFailed, $"{because}. The system said: {Error}");


    // Operations with no result

    public static Task<DriverResult> TryCatchAsync(
        Func<Task> operation,
        string? error = null)
        => TryCatchAsync(operation, exception => error ?? exception.Message);

    public static Task<DriverResult> TryCatchAsync<TException>(
        Func<Task> operation,
        string? error = null)
        where TException : Exception
        => TryCatchAsync(operation,
            exception => exception is TException ? error ?? exception.Message : null);

    public static Task<DriverResult> TryCatchAsync(
        Func<Task> operation,
        Func<Exception, string?> mapException)
        => TryCatchAsync(async () => { await operation(); return Okay; }, mapException);

    // Operations that already produce a DriverResult

    public static Task<DriverResult> TryCatchAsync(
        Func<Task<DriverResult>> operation,
        string? error = null)
        => TryCatchAsync(operation, exception => error ?? exception.Message);

    public static Task<DriverResult> TryCatchAsync<TException>(
        Func<Task<DriverResult>> operation,
        string? error = null)
        where TException : Exception
        => TryCatchAsync(operation,
            exception => exception is TException ? error ?? exception.Message : null);

    public static async Task<DriverResult> TryCatchAsync(
        Func<Task<DriverResult>> operation,
        Func<Exception, string?> mapException)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (mapException(exception) is { } error)
        {
            return Fail(error);
        }
    }
}
```

### Drivers/Channels/IChannel.cs

The capabilities a channel offers. It is a partial interface with one part per domain, so each domain's members live with that domain.

```csharp
namespace acceptance.Drivers.Channels;

// The capabilities one channel offers, split into one part per domain
// (Drivers/<Domain>/IChannel.<Domain>.cs). Required capabilities have no
// default; optional ones default to null, meaning "not supported".
//
// Every part of IChannel, ApiChannel and UiChannel must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
public partial interface IChannel;
```

### Drivers/Channels/ApiChannel.cs

This part declares the constructor. The domain parts use `http` to create their drivers.

```csharp
namespace acceptance.Drivers.Channels;

// Drives the system through its public HTTP API. Every API driver shares
// this channel's HttpClient. Its drivers are wired up one part per domain
// (Drivers/<Domain>/Api/ApiChannel.<Domain>.cs); a capability not wired up
// there is unsupported on this channel and falls back to IChannel's null default.
public partial class ApiChannel(HttpClient http) : IChannel;
```

### Drivers/Channels/UiChannel.cs

Every UI driver shares one page, so they all act in the same browser session.

```csharp
using Microsoft.Playwright;

namespace acceptance.Drivers.Channels;

// Drives the system through the web app in a browser, as a user would.
// Every UI driver shares this channel's page, so they all act in one browser
// session. Its drivers are wired up one part per domain
// (Drivers/<Domain>/Ui/UiChannel.<Domain>.cs); a capability not wired up there
// is unsupported on this channel and falls back to IChannel's null default.
public partial class UiChannel(IPage page) : IChannel;
```

### Drivers/Todos/ITodoDriver.cs

The required todo capability: every channel implements it.

```csharp
namespace acceptance.Drivers.Todos;

public interface ITodoDriver
{
    Task<DriverResult> AddTodo(string title);
    Task<DriverResult> CompleteTodo(string title);
    Task ConfirmTodoIsShown(string title);
    Task ConfirmTodoIsComplete(string title);
}
```

### Drivers/Todos/ITodoTextInputDriver.cs

An optional capability. Only the UI channel supports it.

```csharp
namespace acceptance.Drivers.Todos;

// Live feedback while a todo's title is being entered, before it is submitted.
public interface ITodoTextInputDriver
{
    Task TypeTodoTitle(string title);
    Task ConfirmTitleTooLongWarningIsShown();
}
```

### Drivers/Todos/IChannel.Todos.cs

The todo domain's part of `IChannel`. A required capability has no default. An optional one defaults to `null`, meaning "not supported", so a channel only names what it can do. The file lives in `Todos/`, but its namespace is `Channels`.

```csharp
using acceptance.Drivers.Todos;

// Part of IChannel (see Drivers/Channels/IChannel.cs), so it shares that namespace.
namespace acceptance.Drivers.Channels;

public partial interface IChannel
{
    ITodoDriver Todos { get; }

    ITodoTextInputDriver? TextInput => null;
}
```

### Drivers/Todos/Api/ApiTodoDriver.cs

The comment at the top is the API contract. You've just designed the endpoints; the agent will build them.

```csharp
using System.Net.Http.Json;

namespace acceptance.Drivers.Todos;

// API contract:
//   GET    /api/todos                  -> 200 [{ id, title, completed }]
//   POST   /api/todos                  -> 201, or 400 ValidationProblemDetails when rejected:
//                                         { errors: { "<field>": ["<message>", ...] } },
//                                         e.g. errors.title = ["A title is required"]
//   POST   /api/todos/{id}/complete    -> 2xx

public class ApiTodoDriver(HttpClient http) : ITodoDriver
{
    public async Task<DriverResult> AddTodo(string title) =>
        await ToResult(await http.PostAsJsonAsync("/api/todos", new { title }));

    public async Task<DriverResult> CompleteTodo(string title)
    {
        var todo = await Find(title);
        if (todo is null)
            return DriverResult.Fail($"No todo titled '{title}'");

        return await ToResult(await http.PostAsync($"/api/todos/{todo.Id}/complete", null));
    }

    public async Task ConfirmTodoIsShown(string title) =>
        Assert.True(await Find(title) is not null, $"Expected '{title}' in GET /api/todos");

    public async Task ConfirmTodoIsComplete(string title) =>
        Assert.True((await Find(title))?.Completed == true, $"Expected '{title}' to be completed");

    private static async Task<DriverResult> ToResult(HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? DriverResult.Okay
            : DriverResult.Fail(await BodyOrProblem(response));
    
    private static async Task<string> BodyOrProblem(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
            return await response.Content.ReadAsStringAsync();

        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        return problem?.Errors?.Values.SelectMany(messages => messages).FirstOrDefault()
            ?? problem?.Detail
            ?? problem?.Title
            ?? "";
    }

    private async Task<TodoDto?> Find(string title) =>
        (await http.GetFromJsonAsync<TodoDto[]>("/api/todos"))!
            .FirstOrDefault(todo => todo.Title == title);

    private record TodoDto(int Id, string Title, bool Completed);
    private record ProblemDto(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
```

### Drivers/Todos/Api/ApiChannel.Todos.cs

How the API channel provides the todo domain. It doesn't mention `TextInput`, so the API channel doesn't support it.

```csharp
using acceptance.Drivers.Todos;

// Part of ApiChannel (see Drivers/Channels/ApiChannel.cs), so it shares that namespace.
namespace acceptance.Drivers.Channels;

public partial class ApiChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
```

### Drivers/Todos/Ui/UiTodoDriver.cs

This comment is the UI contract: the `data-testid` attributes Angular must expose. Nothing here depends on CSS classes or layout, so the agent can restyle freely.

```csharp
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace acceptance.Drivers.Todos;

// UI contract (data-testid attributes):
//   new-todo-title     text input for a new todo's title
//   add-todo           submit button; stays enabled, rejections show an error
//   todo-item          one per todo, with data-completed="true" or "false"
//   todo-title         the title text inside a todo-item
//   complete-todo      control inside a todo-item that marks it complete
//   error-message      visible when the last action was rejected

public class UiTodoDriver(IPage page) : ITodoDriver
{
    public async Task<DriverResult> AddTodo(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
        await page.GetByTestId("add-todo").ClickAsync();
        return await Outcome(LocateTodo(title));
    }

    public async Task<DriverResult> CompleteTodo(string title)
    {
        await page.GotoAsync("/");
        await LocateTodo(title).GetByTestId("complete-todo").ClickAsync();
        return await Outcome(LocateCompletedTodo(title));
    }

    public async Task ConfirmTodoIsShown(string title)
    {
        await page.GotoAsync("/");
        await Expect(LocateTodo(title)).ToBeVisibleAsync();
    }

    public async Task ConfirmTodoIsComplete(string title)
    {
        await page.GotoAsync("/");
        await Expect(LocateCompletedTodo(title)).ToBeVisibleAsync();
    }

    // Exact match title, so "" only matches a todo whose title is really blank.
    private ILocator LocateTodo(string title) =>
        page.GetByTestId("todo-item").Filter(new()
        {
            Has = page
                    .GetByTestId("todo-title")
                    .Filter(new()
                    { 
                        HasTextRegex = new Regex($"^{Regex.Escape(title)}$")
                    })
        });

    private ILocator LocateCompletedTodo(string title) =>
        LocateTodo(title).And(page.Locator("[data-completed='true']"));

    // Wait for either the expected change, or the error.
    private async Task<DriverResult> Outcome(ILocator success)
    {
        var error = page.GetByTestId("error-message");
        var result = await DriverResult.TryCatchAsync<TimeoutException>(
            () => success.Or(error).First.WaitForAsync()
        );
        
        if (result.IsFailed)
            return DriverResult.Fail("Timed out waiting for either success or error");

        return await error.IsVisibleAsync()
            ? DriverResult.Fail(await error.InnerTextAsync())
            : DriverResult.Okay;
    }
}
```

### Drivers/Todos/Ui/UiTodoTextInputDriver.cs

More UI contract. `FillAsync` fires an `input` event, which is what the title input listens to, so there's no need to type character by character.

```csharp
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace acceptance.Drivers.Todos;

// UI contract (data-testid attributes):
//   new-todo-title           text input for a new todo's title (see UiTodoDriver)
//   title-too-long-warning   visible while the title being typed is too long,
//                            before the todo is submitted

public class UiTodoTextInputDriver(IPage page) : ITodoTextInputDriver
{
    // FillAsync fires an input event, which is what the title input listens to.
    public async Task TypeTodoTitle(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
    }

    public Task ConfirmTitleTooLongWarningIsShown() =>
        Expect(page.GetByTestId("title-too-long-warning")).ToBeVisibleAsync();
}
```

### Drivers/Todos/Ui/UiChannel.Todos.cs

How the UI channel provides the todo domain, including the optional text input capability.

```csharp
using acceptance.Drivers.Todos;

// Part of UiChannel (see Drivers/Channels/UiChannel.cs), so it shares that namespace.
namespace acceptance.Drivers.Channels;

public partial class UiChannel
{
    public ITodoDriver Todos { get; } = new UiTodoDriver(page);
    public ITodoTextInputDriver? TextInput { get; } = new UiTodoTextInputDriver(page);
}
```

### Dsl/SpecContext.cs

The DSL is organised by **business domain**, not by who is acting: specs say `Todos.Add("Buy milk")`, and later perhaps `Lists.Create(...)` or `Accounts.Register("user: Alice")`. People are data, aliased like everything else, not objects. Each domain's DSL mirrors that domain's drivers, so no single DSL class grows to hold every domain's vocabulary.

Some state belongs to the spec, not to a domain: its aliases, and the outcome of its last action. Every domain's DSL shares this one context.

```csharp
using acceptance.Drivers;

namespace acceptance.Dsl;

// What one spec carries across every domain's DSL: its aliases, and the
// outcome of its last action, whichever domain that action was in.
public class SpecContext
{
    public Alias Alias { get; } = new();
    public DriverResult? LastResult { get; set; }
}
```

### Dsl/DslBase.cs

What every domain's DSL has in common. Each action comes in two forms: `Add` must succeed and fails the test on that line if it doesn't, while `AttemptToAdd` just tries and records the outcome, so the next line can check that the rejection was the right one. `ConfirmErrorIsShown` checks that outcome, whichever domain the action was in. Confirmations are always lines of their own.

```csharp
using acceptance.Drivers;

namespace acceptance.Dsl;

// The base for each business domain's DSL: Todos now, Lists or Accounts later.
// Every domain shares the spec's context, so a spec can act in one domain and
// check the outcome from any of them.
public abstract class DslBase(SpecContext context)
{
    protected Alias Alias => context.Alias;

    protected DriverResult? LastResult => context.LastResult;

    protected void Record(DriverResult result) => context.LastResult = result;

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(context.LastResult);
        Assert.True(context.LastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, context.LastResult.Error);
        return Task.CompletedTask;
    }
}
```

### Dsl/Todos/TodosDsl.cs

The todo vocabulary. Specs reach it as `Todos`, so method names don't repeat the word. The DSL takes the whole channel and reaches each capability through it. If a spec uses an optional capability on a channel that doesn't have it, the error says how to fix the spec.

```csharp
using acceptance.Drivers.Channels;
using acceptance.Drivers.Todos;

namespace acceptance.Dsl.Todos;

// The todo domain's vocabulary: everything a spec can say about todos.
// Specs reach it as Todos, so methods don't repeat the word: Todos.Add("Buy milk").
public class TodosDsl(IChannel channel, SpecContext context) : DslBase(context)
{
    private ITodoDriver Driver => channel.Todos;

    private ITodoTextInputDriver TextInput => channel.TextInput
        ?? throw new InvalidOperationException(
            "This spec uses live text input, which this channel does not support. " +
            "Limit it to the channels that do, e.g. [Fact, Channels(Channel.Ui)].");

    public Task AttemptToAdd(string title) =>
        AttemptToAddAliased(Alias.For(title));

    public Task AttemptToAddWithTitleOfLength(int length) =>
        AttemptToAddAliased(AliasedTitleOfLength(length));

    public async Task Add(string title)
    {
        await AttemptToAdd(title);
        LastResult!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToComplete(string title) =>
        Record(await Driver.CompleteTodo(Alias.For(title)));

    public async Task Complete(string title)
    {
        await AttemptToComplete(title);
        LastResult!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmShown(string title) =>
        Driver.ConfirmTodoIsShown(Alias.For(title));

    public Task ConfirmComplete(string title) =>
        Driver.ConfirmTodoIsComplete(Alias.For(title));

    // Live text input

    public Task TypeTitleOfLength(int length) =>
        TextInput.TypeTodoTitle(AliasedTitleOfLength(length));

    public Task ConfirmWarnedTitleIsTooLong() =>
        TextInput.ConfirmTitleTooLongWarningIsShown();

    // Helpers

    private async Task AttemptToAddAliased(string aliasedTitle) =>
        Record(await Driver.AddTodo(aliasedTitle));

    // The alias suffix counts towards the length, so the system sees exactly `length` characters.
    private string AliasedTitleOfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, Alias.SuffixLength);
        return Alias.For(new string('a', length - Alias.SuffixLength));
    }
}
```

### Infrastructure/AcceptanceTestBase.cs

Every spec class inherits from this. It builds the channel for this run and gives each domain's DSL the channel and the spec's shared context. A new domain adds one property here.

```csharp
using Microsoft.Playwright;
using acceptance.Dsl;
using acceptance.Dsl.Todos;
using acceptance.Drivers.Channels;

namespace acceptance.Infrastructure;

public abstract class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    // One property per business domain. Specs read like Todos.Add("Buy milk").
    protected TodosDsl Todos { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        IChannel channel = AcceptanceChannel.Current switch
        {
            Channel.Api => CreateApiChannel(),
            Channel.Ui => await CreateUiChannel(),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
        var context = new SpecContext();
        Todos = new TodosDsl(channel, context);
    }

    private static IChannel CreateApiChannel() =>
        new ApiChannel(new HttpClient { BaseAddress = new Uri(BaseUrl) });

    private async Task<IChannel> CreateUiChannel()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        var page = await _browser.NewPageAsync(new() { BaseURL = BaseUrl });
        page.SetDefaultTimeout(5_000);
        return new UiChannel(page);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
```

Run `dotnet build acceptance`. It should compile with no tests yet. Commit.

## Step 5: Roles and guardrails

The agent must never be able to make a spec pass by changing the spec, a driver or the checks themselves. And the agent that helps you *write* a spec should never be the one that implements it: if it heard your design conversation, it would fill gaps in the spec from memory, and the spec would stop being the whole contract.

So every Claude Code session has one **role**:

| Role | May change | Typical work |
| --- | --- | --- |
| **Spec agent** (`spec-agent`) | `acceptance/Specs`, `acceptance/Dsl`, `acceptance/Drivers` | Write specs with you, grow the DSL, design driver contracts, watch specs fail |
| **App agent** (`app-agent`) | `api/`, `api.Tests/`, `web/` | Make a committed, failing spec pass |
| **Harness agent** (`harness-agent`) | The harness, scripts, Claude Code settings, deployment files and this guide | Improve the template, on the `template` branch |
| **Free agent** (`free-agent`) | Anything | Cross-cutting work; changes that cross the other agents' boundaries are reported to you |

Every role can read everything and run the tests. The roles only limit the agent: your own edits are never blocked.

### Setting a role

A role belongs to a **worktree**. The quickest way to set it is from the Claude Code chat itself, where these commands tab-complete:

| Command | What it does |
| --- | --- |
| `/spec-agent`, `/app-agent`, `/harness-agent`, `/free-agent` | Sets the role for the next session in this worktree |
| `/role` | Shows this session's role, the next session's role and the last session's role |
| `/role none` | Clears the role |

Then type `/clear` to start a fresh session in that role. A session keeps the role it started with until it ends, even if the role is changed meanwhile: the commands only set the role for the *next* session, and `/clear` starts it.

The role commands are handled by a hook that only runs on what you type, before the agent sees anything, so the agent can't change roles; it only repeats the result to you. Each command is a small file in `.claude/commands/`. The same is available in a terminal, which works without Claude Code:

```powershell
pwsh -File scripts/set-role.ps1             # shows the roles and the choices
pwsh -File scripts/set-role.ps1 spec-agent  # sets the role for the next session
pwsh -File scripts/set-role.ps1 none        # clears it
```
 Switching roles is always a fresh session, so nothing from a spec conversation reaches the app agent. With no role set, the agent can read and discuss but not change anything, and it tells you how to set one.

The role is kept in the worktree's private git folder, so it's never committed and each worktree has its own. To run a spec agent and an app agent side by side, give each its own worktree and open each in its own VS Code window:

```bash
git worktree add ../todo-implement -b implement   # a second worktree on its own branch
```

A branch can only be checked out in one worktree, so the app agent works on its own branch, which you merge back after review. Keep to one active Claude Code session per worktree, because each session judges changes in the working tree as its own.

### How the roles are enforced

Hooks enforce the roles, so they don't depend on the agent remembering them:

- **At session start**, the role is locked for the session and the agent is given its role instructions. It states its role in its first reply.
- **Before every edit**, an edit outside the role's files is blocked, as is any edit inside the git folder or in another worktree. Shell commands that mention the role settings, or that commit or push, are blocked too.
- **When the agent finishes a turn**, anything changed during the turn that the role doesn't own is caught, including changes made through shell commands. The agent is told to undo them, taking care not to discard your own uncommitted work. If it doesn't, it is allowed to finish and you see a warning. Free-agent sessions are never blocked, but you see what they changed across the other agents' files.
- **Before work counts as done**, `check-done.ps1` fails if any uncommitted change falls outside the role of the last session started in the worktree: the role the work was done under. A role set for the next session doesn't change how the work already in the tree is judged.

An agent with a shell can always find *some* way to write a file, for example in another worktree through a shell command. The hooks block the obvious routes and catch the rest at the end of each turn; your review is the final gate. Each hook takes under a second, mostly PowerShell starting up.

### The files


**`CLAUDE.md`**

The working agreement every session shares, whatever its role. Claude Code reads it at the start of every session.

```markdown
# Working agreement

## Roles
- Every session has one role: spec-agent, app-agent, harness-agent or
  free-agent. The human sets it per worktree, with a role command such
  as `/spec-agent` or with `scripts/set-role.ps1`, and it is fixed for
  the whole session.
- Your role and its instructions are given at the start of the session.
  State your role in your first reply.
- `.claude/roles.json` says which files each role may edit, and hooks
  enforce it. Edit only files your role owns. If a hook blocks you, don't
  look for another way: stop and tell the human which role should make
  the change.
- Never change your role, the role settings or the hooks.
- Without a role you may read, explain and discuss, but not change files.

## Always
- Never commit or push. The human commits after review.
- The drivers in `acceptance/Drivers` define the contract: API routes,
  JSON shape and the UI's `data-testid` attributes.
- Specs describe behaviour in domain language. Every spec must pass on
  every channel unless it is marked `[Fact, Channels(...)]`.
```

**`.claude/roles.json`**

The ownership table: the single source of truth for who may change what. A folder entry ends in `/`. When entries overlap, the most specific wins, which is how `api/Dockerfile` belongs to the harness agent while the rest of `api/` belongs to the app agent. A path in no entry belongs to nobody, so only a free agent can change it; `set-role.ps1` refuses to run if two roles claim the same entry. `worksOnTemplate` says whether an agent's work belongs on the `template` branch; the session start warns when a worktree's branch doesn't match.

```json
{
  "templateBranch": "template",
  "//": "worksOnTemplate: true = this agent's work belongs on the template branch; false = it doesn't; absent = either. The session start warns on a mismatch.",
  "roles": {
    "spec-agent": {
      "summary": "Helps the human write executable specifications. Never implements.",
      "worksOnTemplate": false,
      "owns": [
        "acceptance/Specs/",
        "acceptance/Dsl/",
        "acceptance/Drivers/"
      ]
    },
    "app-agent": {
      "summary": "Makes a committed, failing spec pass. Never touches specs or the harness.",
      "worksOnTemplate": false,
      "owns": [
        "api/",
        "api.Tests/",
        "web/"
      ]
    },
    "harness-agent": {
      "summary": "Improves the harness and the template. Writes no specs or app code.",
      "worksOnTemplate": true,
      "owns": [
        "acceptance/Infrastructure/",
        "acceptance/Todo.AcceptanceTests.csproj",
        ".claude/",
        "scripts/",
        "CLAUDE.md",
        "README.md",
        "Executable Specs with an AI Agent ToDo App Walkthrough.md",
        ".editorconfig",
        ".gitattributes",
        ".gitignore",
        "docker-compose.yml",
        "api/Dockerfile",
        "api/.dockerignore",
        "web/Dockerfile",
        "web/.dockerignore",
        "web/nginx.conf"
      ]
    },
    "free-agent": {
      "summary": "No ownership limits, for cross-cutting work. Cross-agent changes are reported.",
      "unrestricted": true
    }
  }
}
```

**`.claude/settings.json`**

Wires the hooks up. `$CLAUDE_PROJECT_DIR` keeps them working if the agent changes directory. The `ask` rules mean even harness-agent and free-agent sessions need your approval for each change to the enforcement itself.

```json
{
  "permissions": {
    "ask": [
        "Edit(/.claude/settings.json)",
        "Edit(/.claude/roles.json)",
        "Edit(/.claude/hooks/roles.ps1)",
        "Edit(/.claude/hooks/role-hooks.ps1)",
        "Edit(/scripts/set-role.ps1)",
        "Edit(/scripts/check-protected-files.ps1)"
    ]
  },
  "hooks": {
    "SessionStart": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "pwsh -NoProfile -File \"$CLAUDE_PROJECT_DIR/.claude/hooks/role-hooks.ps1\" -Event SessionStart"
          }
        ]
      }
    ],
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "pwsh -NoProfile -File \"$CLAUDE_PROJECT_DIR/.claude/hooks/role-hooks.ps1\" -Event UserPromptSubmit"
          }
        ]
      }
    ],
    "PreToolUse": [
      {
        "matcher": "Edit|Write|MultiEdit|NotebookEdit|Bash",
        "hooks": [
          {
            "type": "command",
            "command": "pwsh -NoProfile -File \"$CLAUDE_PROJECT_DIR/.claude/hooks/role-hooks.ps1\" -Event PreToolUse"
          }
        ]
      }
    ],
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "pwsh -NoProfile -File \"$CLAUDE_PROJECT_DIR/.claude/hooks/role-hooks.ps1\" -Event Stop"
          }
        ]
      }
    ]
  }
}
```

**`.claude/roles/spec-agent.md`**

Role instructions, given to the agent at session start. The spec agent's job is to make specs good and to make them fail for the right reason.

```markdown
You help the human write executable specifications. You never implement them.

- Work in the spec layers: specs in `acceptance/Specs`, the vocabulary in
  `acceptance/Dsl`, and the contract in `acceptance/Drivers`. Keep each
  layer to its own job: specs say what, the DSL gives the words, drivers
  say how each channel reaches the system.
- Read the app code only to understand current behaviour. Never shape a
  spec around how the code happens to work.
- When a spec needs new DSL methods or driver contract (endpoints, status
  codes, `data-testid` values), propose them and explain the decisions
  they force. Ask about anything the human hasn't decided.
- Prefer specs for every channel. Use `[Fact, Channels(...)]` only for
  behaviour a channel genuinely lacks, and split a business rule from its
  channel-specific feedback.
- Check your work: `dotnet build acceptance` must pass, and
  `pwsh -File scripts/run-acceptance-tests.ps1` must show each new spec
  failing because the behaviour is missing, not because of broken plumbing.
- Never make a spec pass. If a new spec already passes, say so.
- When a spec is ready, tell the human to review and commit it. The
  app-agent works only from committed specs.
```

**`.claude/roles/app-agent.md`**

The app agent works from the committed spec alone, with the testing rules that used to be in `CLAUDE.md`.

```markdown
You make a committed, failing acceptance spec pass. You never change specs
or the harness.

- Implement specs with the `implement-spec` skill. A task is done when
  `pwsh -File scripts/check-done.ps1` passes.
- The committed spec and the driver contracts are the whole requirement.
  If they are ambiguous or leave a decision open, stop and ask. Don't fill
  gaps with guesses.
- Test-drive all logic: write each unit test before its code, and cover
  every branch, boundary, invalid input and error path. Every test must
  assert something meaningful.
- API tests live in `api.Tests/` (`Todo.Api.Tests`); web tests use the
  Angular project's own setup.
- Add narrow integration tests (one component, one real dependency) for
  persistence against a real database, framework configuration and
  external adapters. Never re-test what acceptance specs cover.
- Deployment files (Dockerfiles, `docker-compose.yml`, `web/nginx.conf`)
  belong to the harness-agent. If you need a change there, ask.
```

**`.claude/roles/harness-agent.md`**

The harness agent changes the rules for every role, including its own, so its changes to the enforcement always ask first.

```markdown
You improve the harness and the template: the acceptance infrastructure,
scripts, Claude Code settings, role definitions, deployment files and the
guide. You don't write specs or app code.

- Your work belongs on the `template` branch. Projects get it by
  merging `template` in.
- Changes to the enforcement itself (`.claude/settings.json`,
  `.claude/roles.json` and the role scripts) ask the human for approval
  each time. Explain what each such change loosens or tightens.
- Test script changes before calling them done, including the failure
  cases, and say what you couldn't test.
- The guide shows some files in full. When you change one of them,
  update the guide to match.
- Every path should belong to exactly one role. When you add a file or
  folder, add it to `.claude/roles.json`.
- When you add or rename a role, add or rename its command in
  `.claude/commands/` and its instructions in `.claude/roles/`.
```

**`.claude/roles/free-agent.md`**

The escape hatch. Use it for the odd cross-cutting change; if your team reaches for it often, the ownership table probably needs adjusting.

```markdown
You have no ownership limits. This role is for cross-cutting work that
doesn't fit one role.

- Changes that cross role boundaries are reported to the human at the end
  of each turn.
- Keep spec changes and implementation separable: say which of your
  changes belong to which role, so the human can review and commit them
  apart. A spec and the code that passes it, written in one conversation,
  no longer check each other.
- Changes to the enforcement itself still ask the human for approval.
- If the work fits a single role, suggest using that role instead.
```

**`.claude/skills/implement-spec/SKILL.md`**

A Claude Code skill: the app agent's procedure for making a failing spec pass. It runs the readiness check for the spec you named, confirms the spec fails for the right reason, implements to the definition of done, and says when to stop and ask rather than push on. You start it with `/implement-spec <SpecName>`.

```markdown
---
name: implement-spec
description: Make a failing acceptance spec pass. Use when asked to implement an acceptance spec or feature.
---

# Implement a failing acceptance spec

Only the app-agent uses this skill. If your session's role is
anything else, stop and tell the human.

## When to stop
At any point, stop and report instead of continuing if:
- the same failure persists after three genuinely different approaches;
- passing would require changing a spec, a driver contract or a guardrail;
- the spec looks contradictory, ambiguous or impossible to satisfy;
- you need a decision the spec doesn't cover.
Report what you tried, what failed and why, and what you need from the
human.

When you stop, leave your changes uncommitted for the human to review.
Before reporting, check that the API and web projects still build and
that `pwsh -File scripts/check-protected-files.ps1` passes. If your latest
change broke the build, undo that change only.

## Steps
1. Identify the target spec: the spec method named in the request. If none
   is named, stop and ask which one. Run
   `pwsh -File scripts/check-ready-to-implement.ps1 -Spec <SpecName>`. If it fails, stop and
   report its output.
2. Read the target spec and the contract comments in the driver files.
3. Run `pwsh -File scripts/run-acceptance-tests.ps1`. Confirm the target spec fails
   because the behaviour is missing, not because of broken plumbing.
   If it fails for any other reason, or already passes, stop and report.
4. Implement in small steps, following the testing rules in CLAUDE.md,
   until the definition of done in CLAUDE.md is met.
5. Report what changed, any design decisions you made, and anything the
   spec didn't cover that a human should decide.
```

### The scripts

The scripts are PowerShell, so they run the same on Windows, macOS and Linux with `pwsh`. They're in the template repository; they're too long to type in, but worth reading.

`scripts/` holds only things you run, so it reads as a list of what you can do. The agent uses some of them too:

| Script | What it does |
| --- | --- |
| `scripts/set-role.ps1` | Sets the role for this worktree's next session, or shows the roles; the terminal version of the role commands |
| `scripts/run-acceptance-tests.ps1` | Starts Docker if needed, deploys fresh containers, runs every spec on each channel |
| `scripts/check-ready-to-implement.ps1` | Checks the tree is clean and the named spec is committed |
| `scripts/check-done.ps1` | The definition of done: the protected-files check, all unit tests and all acceptance tests |
| `scripts/check-protected-files.ps1` | Fails if an uncommitted change is outside the role of the last session started here |

The machinery only Claude Code runs lives beside the settings that wire it up:

| Script | What it does |
| --- | --- |
| `.claude/hooks/role-hooks.ps1` | The four hooks that enforce the session's role |
| `.claude/hooks/roles.ps1` | Shared by the hooks and the role scripts: reads `roles.json`, works out who owns a path, tracks what changed |

`run-acceptance-tests.ps1` uses `--force-recreate`, so every run gets fresh containers and data doesn't build up across runs. Within a run both channels share one system, which is fine because aliasing keeps specs apart. If Docker isn't running, it starts Docker Desktop and waits; to avoid the wait, turn on *Start Docker Desktop when you sign in* in Docker Desktop's general settings.

To run one channel by itself, for example while writing a spec, start the stack and set the channel for that run:

```powershell
docker compose up -d --build
$env:ACCEPTANCE_CHANNEL = "ui"; dotnet test acceptance
```

On a real team the same checks run in CI, with a `CODEOWNERS` entry putting each role's files behind the right reviewers. Commit all of these files before starting the next step.

## Step 6: First loop, add a todo

This is the loop you'll repeat for every feature: write a spec, watch it fail, commit, hand it over, review.

**1. Write the spec** in `acceptance/Specs/AddingTodos.cs`. Write this first one yourself. Later, a spec agent can help: type `/spec-agent`, then `/clear`, and talk the feature through.

```csharp
using acceptance.Infrastructure;

namespace acceptance.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddATodo()
    {
        await Todos.Add("Buy milk");
        await Todos.ConfirmShown("Buy milk");
    }
}
```

**2. Watch it fail, for the right reason.** Deploy the system and run both channels:

```bash
pwsh -File scripts/run-acceptance-tests.ps1
```

The API run should fail at `AddTodo` with "Could not add todo 'Buy milk'" and a 404 from the API. The UI run should time out looking for `new-todo-title`. If it fails anywhere else, your plumbing has a bug; fix that first.

**3. Commit the spec.** `git add -A && git commit -m "Spec: user can add a todo"`

**4. Hand it to the agent.** In Claude Code, make this worktree's role the app agent and start a fresh session:

```text
/app-agent
/clear
```

Then give it something like:

```text
/implement-spec UserCanAddATodo. Keep it simple: in-memory storage
is fine for now.
```

The skill and the app agent's role instructions already tell it where the contract lives, what it may change and when it's done, so the prompt only needs the spec and any design steer.

**5. Review.**

- [ ] `pwsh -File scripts/check-done.ps1` passes when you run it yourself, not just when the agent says so. It includes the protected-files check.
- [ ] The agent added unit tests in the API and web projects
- [ ] The diff is something you'd accept from a colleague

Then commit. Notice what you *didn't* do: decide how the API stores todos, or how the Angular component is structured. Those are the agent's choices, and the spec doesn't care.

## Step 7: Unhappy path, blank titles are rejected

Add a second spec to `AddingTodos.cs`:

```csharp
[Fact]
public async Task CannotAddATodoWithoutATitle()
{
    await Todos.AttemptToAdd("");
    await Todos.ConfirmErrorIsShown("A title is required");
}
```

Run `pwsh -File scripts/run-acceptance-tests.ps1`. Right now the app probably accepts blank titles, so expect "Expected the last action to fail, but it succeeded." on both. In the UI run, the driver spotted a todo with an empty title appearing. That's the exact-match locator doing its job.

Note that `"A title is required"` pins the exact wording on purpose. The API must return it in the 400 body's `errors`, as the API driver's contract describes, and the UI must show it in `error-message`. If you'd rather not fix the wording, make the DSL method check only that *some* error was shown.

Commit, then hand it over in a new app-agent session:

```text
/implement-spec CannotAddATodoWithoutATitle. Validate in the API; the
UI may also validate, but must show the error in error-message.
```

Run the same review checklist as step 6.

## Step 8: Complete a todo, then design one yourself

The DSL and drivers already support completing, so this one is just a spec. Create `acceptance/Specs/CompletingTodos.cs`:

```csharp
using acceptance.Infrastructure;

namespace acceptance.Specs;

public class CompletingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanCompleteATodo()
    {
        await Todos.Add("Walk the dog");
        await Todos.Complete("Walk the dog");
        await Todos.ConfirmComplete("Walk the dog");
    }
}
```

Run the loop exactly as before.

**Your turn: a feature where you design the contract.** Pick one, such as deleting a todo or rejecting a duplicate title. This time nothing exists yet, so work outside in:

1. Write the spec first, using DSL methods that don't exist yet, phrased how you'd say it out loud.
2. Add those methods to the domain's DSL, `TodosDsl`. A new area of the business gets its own DSL class and drivers instead.
3. Add them to `ITodoDriver`, then decide the contract: which endpoint and status code for the API driver, which `data-testid` for the UI driver. Update the contract comments.
4. Watch it fail on both channels for the right reason, commit, hand over.

**Or try a spec for one channel only.** The plumbing already has an optional capability, live feedback while typing, that only the UI channel supports. Warn the user while they type a title that's too long:

```csharp
[Fact, Channels(Channel.Ui)]
public async Task UserIsWarnedWhileTypingATitleLongerThan255Utf16Characters()
{
    await Todos.TypeTitleOfLength(256);
    await Todos.ConfirmWarnedTitleIsTooLong();
}
```

On the API run it is reported as skipped; on the UI run it fails until `title-too-long-warning` exists. Two rules keep single-channel specs honest:

- **Split the rule from the feedback.** "A title can't be longer than 255 characters" is a business rule, so it belongs in a spec for every channel, using `Todos.AttemptToAddWithTitleOfLength`. Only the warning while typing is UI-only. Otherwise the API would accept what the UI forbids.
- **New optional capabilities follow the same shape:** an interface in the domain folder, a nullable member with a `null` default in `IChannel.<Domain>.cs`, and a driver plus one line of wiring for each channel that supports it.

This is where most of the real thinking lives, and where a spec agent earns its place: it can draft the spec, DSL and driver changes with you, and point out the decisions they force. You'll notice that writing the driver forces decisions a prose spec would leave vague.

## Step 9: Prove the point

This is the payoff: big implementation changes with zero changes to `acceptance/`. Try at least the first two.

1. **Swap the storage.** Ask the agent to replace in-memory storage with EF Core and SQLite, persisted in a Docker volume. Every spec should still pass untouched.
2. **Restyle the UI.** Ask for a full visual redesign or a component library. The drivers rely only on test IDs, so the specs survive.
3. **Test the lock.** Write a spec the app can't reasonably satisfy, such as an assertion with a typo in the expected message, and ask the agent to "make the build green". It should stop and flag the spec rather than edit it. If it tries anyway, the role hooks will block or catch it. Then try the opposite with a spec agent: ask it to implement the feature its spec describes.

If a spec breaks during 1 or 2, look at why. It usually means the spec or driver leaked an implementation detail, which is exactly the feedback this approach is meant to give you.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| "Docker isn't running and couldn't be started" | Start Docker Desktop yourself; it may need an update or a sign-in prompt answered |
| "Docker Desktop couldn't be started from here" | Your Docker Desktop predates its CLI, or you use Docker Engine; start Docker yourself |
| Every API call returns 502 | The API container isn't up yet; wait a few seconds, or check `docker compose logs api` |
| UI driver times out at the add button | The agent disabled the button for blank input; the contract says it stays enabled |
| UI tests pass but the API channel fails | Logic lives only in Angular; validation must also be in the API |
| Playwright says no browser is installed | Re-run the `playwright.ps1 install chromium` step after any package upgrade |
| Old behaviour after the agent's changes | You started the stack by hand without `--build`; `scripts/run-acceptance-tests.ps1` always rebuilds |
| A spec is reported as skipped | It's limited to other channels with `[Channels(...)]`, which is expected |
| `check-ready-to-implement.ps1` fails | Commit everything, including the spec, and name the spec method exactly |
| The agent says no role is set, or every edit is blocked | Type `/<role>`, for example `/app-agent`, then `/clear` |
| The agent still has its old role | Sessions keep the role they started with; type `/clear` to start a fresh one |
| A role command only makes the agent suggest a terminal command | The role hook didn't run; check `.claude/settings.json` is in place and start a new session, or use `scripts/set-role.ps1` |
| An edit is blocked as belonging to another role | Working as designed: use a session with that role, or add the path to `.claude/roles.json` if it's in the wrong role |
| A new file is blocked as not owned by any role | Add it, or its folder, to the right role in `.claude/roles.json` (a harness-agent change) |
| The agent is told it changed files outside its role | It changed one during its turn, perhaps through a shell command; it should undo that and explain why |
| A "Files outside the … role changed" warning appears | The agent didn't undo a change after being told; review the diff and revert it yourself |
| `check-protected-files.ps1` fails in `check-done.ps1` | An uncommitted change is outside the role of the last session started here, perhaps your own edit; commit or move it first |

## Questions to reflect on

- How much of your time went on specs and drivers, versus reviewing the agent's code?
- Where did writing a driver force a decision you'd otherwise have left vague?
- Which review checks would you trust enough to automate, and which need a human?
