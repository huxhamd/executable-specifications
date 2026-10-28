# Executable Specs with an AI Agent: ToDo App Walkthrough

Oct 22, 2026 · @Daniel Huxham

## What you'll build

You'll build a tiny ToDo app (Angular front end, C# REST API) where you never write the app code yourself. You write executable specifications, lock them, and let Claude Code make them pass. Plan on two or three evenings.

By the end you'll have practised the full loop four times, including one unhappy path, and you'll have proved the payoff: swapping the storage layer without touching a single spec.

**Who owns what**

| Folder | What it holds | Owner |
| --- | --- | --- |
| `acceptance/Specs` | The executable specifications | You, locked from the agent |
| `acceptance/Dsl` | The domain vocabulary the specs speak | You, agent may propose |
| `acceptance/Drivers` | The contract: API routes and JSON, UI test IDs | You, agent may propose |
| `api/` | ASP.NET Core API, its unit tests | Agent |
| `web/` | Angular app, its unit tests | Agent |
| `docker-compose.yml` and Dockerfiles | The deployable system | You set up once |
| `CLAUDE.md`, `.claude/`, `scripts/` | The working agreement, guardrails and run scripts | You, locked from the agent |

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
    UserDsl.cs                  the vocabulary specs use
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

Each folder level splits on one thing. Under `Drivers/`, folders are domains (`Todos/`, later perhaps `Lists/`), and inside a domain, folders are channels. `Channels/` holds everything about a channel that isn't specific to one domain.

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

### Dsl/UserDsl.cs

Each action comes in two forms. `AddTodo` must succeed and fails the test on that line if it doesn't. `AttemptToAddTodo` just tries and records the outcome, so the next line can check that the rejection was the right one. Confirmations are always lines of their own.

The DSL takes the whole channel and reaches each capability through it. If a spec uses an optional capability on a channel that doesn't have it, the error says how to fix the spec.

```csharp
using acceptance.Drivers;
using acceptance.Drivers.Channels;
using acceptance.Drivers.Todos;

namespace acceptance.Dsl;

public class UserDsl(IChannel channel, Alias alias)
{
    private DriverResult? _lastResult;

    private ITodoDriver Todos => channel.Todos;

    private ITodoTextInputDriver TextInput => channel.TextInput
        ?? throw new InvalidOperationException(
            "This spec uses live text input, which this channel does not support. " +
            "Limit it to the channels that do, e.g. [Fact, Channels(Channel.Ui)].");

    public Task AttemptToAddTodoWithTitleOfLength(int length) =>
        AttemptToAddAliasedTodo(AliasedTitleOfLength(length));

    public Task AttemptToAddTodo(string title) =>
        AttemptToAddAliasedTodo(alias.For(title));

    public async Task AddTodo(string title)
    {
        await AttemptToAddTodo(title);
        _lastResult!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToCompleteTodo(string title) =>
        _lastResult = await Todos.CompleteTodo(alias.For(title));

    public async Task CompleteTodo(string title)
    {
        await AttemptToCompleteTodo(title);
        _lastResult!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmTodoIsShown(string title) =>
        Todos.ConfirmTodoIsShown(alias.For(title));

    public Task ConfirmTodoIsComplete(string title) =>
        Todos.ConfirmTodoIsComplete(alias.For(title));

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_lastResult);
        Assert.True(_lastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, _lastResult.Error);
        return Task.CompletedTask;
    }

    // Live text input

    public Task TypeTodoTitleOfLength(int length) =>
        TextInput.TypeTodoTitle(AliasedTitleOfLength(length));

    public Task ConfirmWarnedTitleIsTooLong() =>
        TextInput.ConfirmTitleTooLongWarningIsShown();

    // Helpers

    private async Task AttemptToAddAliasedTodo(string aliasedTitle) =>
        _lastResult = await Todos.AddTodo(aliasedTitle);

    // The alias suffix counts towards the length, so the system sees exactly `length` characters.
    private string AliasedTitleOfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, Alias.SuffixLength);
        return alias.For(new string('a', length - Alias.SuffixLength));
    }
}
```

### Infrastructure/AcceptanceTestBase.cs

Every spec class inherits from this. It builds the channel for this run and hands it to the DSL.

```csharp
using Microsoft.Playwright;
using acceptance.Dsl;
using acceptance.Drivers.Channels;

namespace acceptance.Infrastructure;

public abstract class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    protected UserDsl User { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        IChannel channel = AcceptanceChannel.Current switch
        {
            Channel.Api => CreateApiChannel(),
            Channel.Ui => await CreateUiChannel(),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
        User = new UserDsl(channel, new Alias());
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

## Step 5: Lock the specs and script the loop

The agent must never be able to make a spec pass by changing the spec, a driver or the checks themselves. No single lock is enough, so use several: the working agreement tells the agent the rules, permission rules block its edit tools, and a guardrail script catches anything that slips through a shell command. Scripts then turn "run everything and check it" into one command each, for you and the agent alike. The scripts are PowerShell, so they run the same on Windows, macOS and Linux with `pwsh`.

**`CLAUDE.md`**

The working agreement, at the repo root. Claude Code reads it at the start of every session. It refers to the skill and scripts that follow, and to `api.Tests/`, which the agent creates when it first test-drives API code.

```markdown
# Working agreement

## Ownership
- `acceptance/` and every path denied in `.claude/settings.json` are
  owned by the human. Never modify, delete, skip or rename them. If a
  spec looks wrong or a guardrail blocks you, stop and explain why.
- Never commit. The human commits after review.
- The drivers in `acceptance/Drivers` define the contract: API routes,
  JSON shape and the UI's `data-testid` attributes.

## Testing
- Test-drive all logic: write each unit test before its code, and cover
  every branch, boundary, invalid input and error path. Every test must
  assert something meaningful.
- API tests live in `api.Tests/` (`Todo.Api.Tests`); web tests use the
  Angular project's own setup.
- Add narrow integration tests (one component, one real dependency) for
  persistence against a real database, framework configuration and
  external adapters. Never re-test what acceptance specs cover.

## Done
- Implement acceptance specs with the `implement-spec` skill.
- A task is done when `pwsh -File scripts/verify.ps1` passes.
```

**`.claude/settings.json`**

The `deny` rules block the agent's file-editing tools on everything it must not change: `acceptance/`, this file, the skill and the scripts. The leading `/` anchors a path to the project root. Changes to `CLAUDE.md` need your approval. The `Stop` hook runs the guardrail check every time the agent finishes, so a protected change is caught straight away rather than at review.

```json
{
  "permissions": {
    "deny": [
        "Edit(/acceptance/**)",
        "Edit(/.claude/settings.json)",
        "Edit(/.claude/skills/implement-spec/**)",
        "Edit(/scripts/acceptance.ps1)",
        "Edit(/scripts/check-guardrails.ps1)",
        "Edit(/scripts/preflight.ps1)",
        "Edit(/scripts/verify.ps1)"
    ],
    "ask": [
        "Edit(/CLAUDE.md)"
    ]
  },
  "hooks": {
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "pwsh -NoProfile -File scripts/check-guardrails.ps1"
          }
        ]
      }
    ]
  }
}
```

**`scripts/check-guardrails.ps1`**

The real lock. Edit rules don't cover shell commands, but this does: any uncommitted change to a protected path fails the check. Exit code 2 is what lets the `Stop` hook block the agent from finishing and show it the message. Because the check compares against your last commit, always commit your specs before handing work over.

```powershell
$protected = @(
    "acceptance/",
    ".claude/settings.json",
    ".claude/skills/implement-spec/",
    "scripts/acceptance.ps1",
    "scripts/check-guardrails.ps1",
    "scripts/preflight.ps1",
    "scripts/verify.ps1"
)
$changes = git status --porcelain -- $protected
if ($changes) {
    [Console]::Error.WriteLine("Protected files have uncommitted changes:`n$changes")
    exit 2
}
Write-Host "Guardrails intact: no protected files have changed."
exit 0
```

**`scripts/acceptance.ps1`**

Deploys the system and runs every spec through each channel in turn. `--force-recreate` gives every run fresh containers, so data doesn't build up across runs. Within a run, both channels share the same system, which is fine because aliasing keeps specs apart.

```powershell
docker compose up -d --build --force-recreate
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$failed = @()
foreach ($channel in "api", "ui") {
    Write-Host "`n=== Acceptance tests: $channel ===`n"
    $env:ACCEPTANCE_CHANNEL = $channel
    dotnet test acceptance
    if ($LASTEXITCODE -ne 0) { $failed += $channel }
}
Remove-Item Env:ACCEPTANCE_CHANNEL

if ($failed) {
    Write-Host "`nFailed channels: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "`nAll channels passed" -ForegroundColor Green
```

**`scripts/verify.ps1`**

The definition of done: guardrails, all unit tests and all acceptance tests, with a summary at the end. The agent's task is finished only when this passes, and so is your review.

```powershell
$results = [ordered]@{}

function Invoke-Check([string]$name, [scriptblock]$check) {
    Write-Host "`n=== $name ===`n"
    & $check
    $results[$name] = ($LASTEXITCODE -eq 0)
}

Invoke-Check "Guardrails"       { pwsh -NoProfile -File scripts/check-guardrails.ps1 }
Invoke-Check "API unit tests"   { dotnet test api.Tests }
Invoke-Check "Web unit tests"   { Push-Location web; npx ng test --watch=false; Pop-Location }
Invoke-Check "Acceptance tests" { pwsh -NoProfile -File scripts/acceptance.ps1 }

Write-Host "`n=== Summary ===`n"
foreach ($entry in $results.GetEnumerator()) {
    if ($entry.Value) { Write-Host "PASS  $($entry.Key)" -ForegroundColor Green }
    else              { Write-Host "FAIL  $($entry.Key)" -ForegroundColor Red }
}

if ($results.Values -contains $false) { exit 1 }
Write-Host "`nVerified: all checks passed." -ForegroundColor Green
```

**`scripts/preflight.ps1`**

Checks the starting point before the agent begins: the working tree is clean, and your latest commit adds or changes a spec. That stops the agent starting from uncommitted work, or with nothing to implement.

```powershell
$dirty = git status --porcelain
if ($dirty) {
    [Console]::Error.WriteLine("Working tree is not clean:`n$dirty")
    exit 1
}

$specs = git diff --name-only HEAD~1 HEAD -- acceptance/Specs
if (-not $specs) {
    [Console]::Error.WriteLine("The latest commit doesn't add or change a spec in acceptance/Specs.")
    exit 1
}

Write-Host "Preflight passed. Specs in the latest commit:`n$specs"
```

**`.claude/skills/implement-spec/SKILL.md`**

A Claude Code skill: the agent's procedure for making a failing spec pass. It runs the preflight, confirms the spec fails for the right reason, implements to the definition of done in `CLAUDE.md`, and says when to stop and ask rather than push on. You start it with `/implement-spec`.

```markdown
---
name: implement-spec
description: Make a failing acceptance spec pass. Use when asked to implement an acceptance spec or feature.
---

# Implement a failing acceptance spec

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
that `pwsh -File scripts/check-guardrails.ps1` passes. If your latest
change broke the build, undo that change only.

## Steps
1. Run `pwsh -File scripts/preflight.ps1`. If it fails, stop and report its output.
2. Read the target spec and the contract comments in the driver files.
3. Run `pwsh -File scripts/acceptance.ps1`. Confirm the target spec fails
   because the behaviour is missing, not because of broken plumbing.
   If it fails for any other reason, or already passes, stop and report.
4. Implement in small steps, following the testing rules in CLAUDE.md,
   until the definition of done in CLAUDE.md is met.
5. Report what changed, any design decisions you made, and anything the
   spec didn't cover that a human should decide.
```

To run one channel by itself, for example while writing a spec, start the stack and set the channel for that run:

```powershell
docker compose up -d --build
$env:ACCEPTANCE_CHANNEL = "ui"; dotnet test acceptance
```

On a real team the same checks run in CI, with a `CODEOWNERS` entry putting the protected paths behind review. Commit all of these files before starting the next step.

## Step 6: First loop, add a todo

This is the loop you'll repeat for every feature: write a spec, watch it fail, commit, hand it over, review.

**1. Write the spec** in `acceptance/Specs/AddingTodos.cs`:

```csharp
using acceptance.Infrastructure;

namespace acceptance.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddATodo()
    {
        await User.AddTodo("Buy milk");
        await User.ConfirmTodoIsShown("Buy milk");
    }
}
```

**2. Watch it fail, for the right reason.** Deploy the system and run both channels:

```bash
pwsh -File scripts/acceptance.ps1
```

The API run should fail at `AddTodo` with "Could not add todo 'Buy milk'" and a 404 from the API. The UI run should time out looking for `new-todo-title`. If it fails anywhere else, your plumbing has a bug; fix that first.

**3. Commit the spec.** `git add -A && git commit -m "Spec: user can add a todo"`

**4. Hand it to the agent.** Start `claude` in the repo root and give it something like:

```text
/implement-spec UserCanAddATodo. Keep it simple: in-memory storage
is fine for now.
```

The skill and `CLAUDE.md` already tell it where the contract lives, what it may change and when it's done, so the prompt only needs the spec and any design steer.

**5. Review.**

- [ ] `pwsh -File scripts/verify.ps1` passes when you run it yourself, not just when the agent says so. It includes the guardrail check.
- [ ] The agent added unit tests in the API and web projects
- [ ] The diff is something you'd accept from a colleague

Then commit. Notice what you *didn't* do: decide how the API stores todos, or how the Angular component is structured. Those are the agent's choices, and the spec doesn't care.

## Step 7: Unhappy path, blank titles are rejected

Add a second spec to `AddingTodos.cs`:

```csharp
[Fact]
public async Task CannotAddATodoWithoutATitle()
{
    await User.AttemptToAddTodo("");
    await User.ConfirmErrorIsShown("A title is required");
}
```

Run `pwsh -File scripts/acceptance.ps1`. Right now the app probably accepts blank titles, so expect "Expected the last action to fail, but it succeeded." on both. In the UI run, the driver spotted a todo with an empty title appearing. That's the exact-match locator doing its job.

Note that `"A title is required"` pins the exact wording on purpose. The API must return it in the 400 body's `errors`, as the API driver's contract describes, and the UI must show it in `error-message`. If you'd rather not fix the wording, make the DSL method check only that *some* error was shown.

Commit, then hand it over:

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
        await User.AddTodo("Walk the dog");
        await User.CompleteTodo("Walk the dog");
        await User.ConfirmTodoIsComplete("Walk the dog");
    }
}
```

Run the loop exactly as before.

**Your turn: a feature where you design the contract.** Pick one, such as deleting a todo or rejecting a duplicate title. This time nothing exists yet, so work outside in:

1. Write the spec first, using DSL methods that don't exist yet, phrased how you'd say it out loud.
2. Add those methods to `UserDsl`.
3. Add them to `ITodoDriver`, then decide the contract: which endpoint and status code for the API driver, which `data-testid` for the UI driver. Update the contract comments.
4. Watch it fail on both channels for the right reason, commit, hand over.

**Or try a spec for one channel only.** The plumbing already has an optional capability, live feedback while typing, that only the UI channel supports. Warn the user while they type a title that's too long:

```csharp
[Fact, Channels(Channel.Ui)]
public async Task UserIsWarnedWhileTypingATitleLongerThan255Utf16Characters()
{
    await User.TypeTodoTitleOfLength(256);
    await User.ConfirmWarnedTitleIsTooLong();
}
```

On the API run it is reported as skipped; on the UI run it fails until `title-too-long-warning` exists. Two rules keep single-channel specs honest:

- **Split the rule from the feedback.** "A title can't be longer than 255 characters" is a business rule, so it belongs in a spec for every channel, using `AttemptToAddTodoWithTitleOfLength`. Only the warning while typing is UI-only. Otherwise the API would accept what the UI forbids.
- **New optional capabilities follow the same shape:** an interface in the domain folder, a nullable member with a `null` default in `IChannel.<Domain>.cs`, and a driver plus one line of wiring for each channel that supports it.

This is where most of the real thinking lives. You'll notice that writing the driver forces decisions a prose spec would leave vague.

## Step 9: Prove the point

This is the payoff: big implementation changes with zero changes to `acceptance/`. Try at least the first two.

1. **Swap the storage.** Ask the agent to replace in-memory storage with EF Core and SQLite, persisted in a Docker volume. Every spec should still pass untouched.
2. **Restyle the UI.** Ask for a full visual redesign or a component library. The drivers rely only on test IDs, so the specs survive.
3. **Test the lock.** Write a spec the app can't reasonably satisfy, such as an assertion with a typo in the expected message, and ask the agent to "make the build green". It should stop and flag the spec rather than edit it. If it tries anyway, the permission rules or the guardrail check will catch it.

If a spec breaks during 1 or 2, look at why. It usually means the spec or driver leaked an implementation detail, which is exactly the feedback this approach is meant to give you.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| Every API call returns 502 | The API container isn't up yet; wait a few seconds, or check `docker compose logs api` |
| UI driver times out at the add button | The agent disabled the button for blank input; the contract says it stays enabled |
| UI tests pass but the API channel fails | Logic lives only in Angular; validation must also be in the API |
| Playwright says no browser is installed | Re-run the `playwright.ps1 install chromium` step after any package upgrade |
| Old behaviour after the agent's changes | You started the stack by hand without `--build`; `scripts/acceptance.ps1` always rebuilds |
| A spec is reported as skipped | It's limited to other channels with `[Channels(...)]`, which is expected |
| `preflight.ps1` fails | Commit your spec, and nothing else, before handing over |
| The agent can't finish and mentions protected files | The `Stop` hook found a change under a protected path; review it and revert it |

## Questions to reflect on

- How much of your time went on specs and drivers, versus reviewing the agent's code?
- Where did writing a driver force a decision you'd otherwise have left vague?
- Which review checks would you trust enough to automate, and which need a human?
