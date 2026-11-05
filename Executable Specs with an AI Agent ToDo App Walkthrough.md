# Executable Specs with an AI Agent: ToDo App Walkthrough

Oct 22, 2026 · @Daniel Huxham

## What you'll build

You'll build a tiny ToDo app (Angular front end, C# REST API) where you never write the app code yourself. You write executable specifications, lock them, and let Claude Code make them pass. Plan on two or three evenings.

By the end you'll have practised the full loop four times, including one unhappy path, and you'll have proved the payoff: swapping the storage layer without touching a single spec.

**Who owns what**

You own everything and review everything. The agent works in one of three roles at a time, and each role may change only its own part of the repo:

| Folder | What it holds | Agent role that may change it |
| --- | --- | --- |
| `acceptance.Specs/<Domain>` | The executable specifications | Spec agent |
| `acceptance.Dsl/<Domain>` | The domain vocabulary the specs speak | Spec agent |
| `acceptance.Drivers/<Domain>` | The contract: API routes and JSON, UI test IDs, and how each channel provides them | Spec agent |
| `acceptance.Dsl/Core`, `acceptance.Drivers/Core` | What every domain's DSL and drivers share | Spec agent |
| `api/`, `api.UnitTests/`, `api.IntegrationTests/`, `web/` | The app and its unit and integration tests | App agent |
| `acceptance.Dsl/Core/Wiring`, `acceptance.Drivers/Core/Wiring`, `scripts/`, `.claude/`, `CLAUDE.md`, deployment files, this guide | The harness, guardrails and deployable system | Harness agent |

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

# Acceptance: one project per layer. Drivers first: Playwright, no xUnit...
dotnet new classlib -n Todo.Acceptance.Drivers -o acceptance.Drivers
cd acceptance.Drivers
dotnet add package Microsoft.Playwright
cd ..

# ...then the DSL, which gives the verdicts...
dotnet new classlib -n Todo.Acceptance.Dsl -o acceptance.Dsl
cd acceptance.Dsl
dotnet add package xunit.v3.extensibility.core
dotnet add package xunit.v3.assert
dotnet add reference ../acceptance.Drivers/Todo.Acceptance.Drivers.csproj
cd ..

# ...and the specs, which can only see the DSL
dotnet new xunit -n Todo.Acceptance.Specs -o acceptance.Specs
cd acceptance.Specs
dotnet remove package xunit
dotnet add package xunit.v3.mtp-off
dotnet add package xunit.runner.visualstudio
dotnet add reference ../acceptance.Dsl/Todo.Acceptance.Dsl.csproj
cd ..
```

Each layer gets its own project, so the compiler enforces the layering. The drivers' types are `internal`, and `acceptance.Drivers` lets only the DSL see them: add `<ItemGroup><InternalsVisibleTo Include="Todo.Acceptance.Dsl" /></ItemGroup>` to `acceptance.Drivers/Todo.Acceptance.Drivers.csproj`. In the same file, add `PrivateAssets="compile;contentfiles;analyzers;build"` to the `Microsoft.Playwright` package reference, so only the drivers can compile against Playwright. That's NuGet's default plus `compile`: the specs still get Playwright and its browser driver at runtime. The DSL holds no tests, so it uses xUnit's extensibility and assertion packages rather than the test runner; add `<ItemGroup><Using Include="Xunit" /></ItemGroup>` to its project file, as the specs' already has. The drivers have no xUnit at all, so they can't assert. The `xunit` template still uses xUnit v2. The plumbing needs v3, for the `Channels` attribute's runtime skipping, and v3 4.0 or later, for running specs in the same class in parallel, which is why the package is swapped. `xunit.v3.mtp-off` is v3 for VSTest, which is what `dotnet test` uses unless you opt in to Microsoft Testing Platform; plain `xunit.v3` 4.x only works with the latter. Keep every xUnit package, in the DSL as well as the specs, on the same version. v3 test projects are executables, so add `<OutputType>Exe</OutputType>` to the `PropertyGroup` in `acceptance.Specs/Todo.Acceptance.Specs.csproj`. Then delete the templates' two `Class1.cs` files and `UnitTest1.cs`, and install the browser:

```bash
dotnet build acceptance.Specs
pwsh acceptance.Drivers/bin/Debug/net10.0/playwright.ps1 install chromium
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

This is the one-off setup cost. Type these files in yourself rather than pasting, because the point is to understand each layer. Namespaces are included: the channel types and the test base are split into partial parts across folders, and every part of a partial type must share one namespace and one project. Namespaces read `acceptance.<Core or Domain>.<Layer>`, so `acceptance.Drivers/Todos/` is `acceptance.Todos.Drivers`. `Wiring/` folders don't add to the namespace: a part there shares the namespace of the type it extends.

```
acceptance.Drivers/             the driver layer: how each channel reaches the system
  Core/                         shared by every domain
    DriverResult.cs             what a driver action did
    Wiring/                     one channel per way of reaching the system (harness agent)
      IChannel.cs
      ApiChannel.cs             opens and closes the HttpClient
      UiChannel.cs              shares one browser, opens a session per spec
  Todos/                        one folder per domain: the domain's contract
    ITodoDriver.cs              what every channel can do with todos
    ITodoEntryDriver.cs         live feedback while entering a todo (UI only)
    ApiTodoDriver.cs
    UiTodoDriver.cs             implements both
    Wiring/                     the domain's parts of the channels
      IChannel.Todos.cs
      ApiChannel.Todos.cs
      UiChannel.Todos.cs
acceptance.Dsl/                 the DSL layer: the words specs use
  Core/
    DslBase.cs                  the base for each domain's DSL
    Alias.cs                    unique names per test
    SpecContext.cs              what a spec shares across domains
    Wiring/                     the spec base (harness agent)
      ChannelType.cs            which channel this run uses
      ChannelsAttribute.cs      limits a spec to some channels
      AcceptanceTestBase.cs     picks the channel, holds the spec's context
  Todos/
    TodosDsl.cs                 the todo vocabulary specs use
    Wiring/                     the domain's part of the spec base
      AcceptanceTestBase.Todos.cs   gives specs the Todos property
acceptance.Specs/               the spec layer: the specs, one folder per domain
  Core/
    Wiring/                     how the specs are run (harness agent)
      Parallelization.cs        runs every spec in parallel
  Todos/                        empty for now
```

Each layer is its own project, and each project has a `Core/` folder for what every domain shares and one folder per domain (`Todos/`, later perhaps `Lists/`). Adding a domain is adding one folder in each project.

Every `Core/` and domain folder in `acceptance.Drivers` and `acceptance.Dsl` has the same shape: the layer's own code, and a `Wiring/` folder that connects it to the layer above, one part per domain. In the drivers, the wiring is the channels the DSL is given; in the DSL, it's the spec base that specs inherit. `acceptance.Specs` has nothing above it to connect to, so its only wiring is how the specs are run. Folder names describe the architecture, not who owns it: ownership lives in `.claude/roles.json`, where the harness agent owns each `Core/Wiring/` and the spec agent owns the rest.

The projects reference each other in one direction only, and the compiler enforces the layering:

- **Specs see only the DSL.** `acceptance.Specs` sees what `acceptance.Dsl` makes public: `AcceptanceTestBase`, the DSLs, `ChannelType` and the `Channels` attribute. A spec that tries to skip the DSL doesn't compile.
- **Only the DSL sees the drivers.** Everything in `acceptance.Drivers` is `internal`, and `InternalsVisibleTo` names the DSL as the one project allowed in.
- **Only the drivers see Playwright.** The DSL and the specs get it at runtime, but can't compile against it.
- **Drivers can't give verdicts.** `acceptance.Drivers` has no xUnit, so `Assert` doesn't exist there. Playwright's own assertions do, because they ship in the package the drivers need: `using static Microsoft.Playwright.Assertions;` in a driver means a verdict has crept into the wrong layer. Drivers report, the DSL decides.
A **channel** is one way of reaching the whole system: the public API or the browser. Its driver interfaces are split by domain and by **capability**. A capability every channel must support, like `ITodoDriver`, is required. One only some channels support, like `ITodoEntryDriver` (live feedback while a new todo is being entered, which only a browser has), is optional: channels that don't support it simply don't mention it. Each channel has one driver class per domain, implementing every capability it supports, so `UiTodoDriver` implements both.

### acceptance.Dsl/Core/Wiring/ChannelType.cs

The `ACCEPTANCE_CHANNEL` environment variable chooses the channel, so every spec runs unchanged through the API or the browser.

```csharp
namespace acceptance.Core.Dsl;

// The channels a spec can run through. scripts/run-acceptance-tests.ps1 keeps
// its own list of channel names: add or rename a channel in both places.
public enum ChannelType { Api, Ui }

// The channel this run drives the system through, from ACCEPTANCE_CHANNEL:
// a ChannelType name, in any case.
internal static class AcceptanceChannel
{
    public static ChannelType Current
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("ACCEPTANCE_CHANNEL")?.Trim();
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException(
                    $"Set ACCEPTANCE_CHANNEL to {Choices} before running the acceptance tests.");

            // Matched by name rather than Enum.TryParse, which would also accept
            // numbers ("0") and lists of flags ("Api, Ui").
            var name = Enum.GetNames<ChannelType>()
                .SingleOrDefault(candidate => candidate.Equals(value, StringComparison.OrdinalIgnoreCase));
            return name is not null
                ? Enum.Parse<ChannelType>(name)
                : throw new InvalidOperationException(
                    $"Unknown ACCEPTANCE_CHANNEL '{value}'. Use {Choices}.");
        }
    }

    private static string Choices =>
        string.Join(" or ", Enum.GetNames<ChannelType>().Select(name => $"'{name.ToLowerInvariant()}'"));
}
```

### acceptance.Dsl/Core/Wiring/ChannelsAttribute.cs

Most specs apply to every channel. A spec for behaviour only some channels have, such as a warning shown while typing, is marked `[Fact, Channels(ChannelType.Ui)]`. On other channels it is reported as skipped, so nothing disappears silently. This relies on xUnit v3's runtime skipping.

```csharp
using System.Reflection;
using Xunit.v3;

namespace acceptance.Core.Dsl;

// Limits a spec to the channels it applies to: [Fact, Channels(ChannelType.Ui)].
// Specs without it run on every channel, which should stay the default.
// On other channels the spec is reported as skipped, never silently dropped.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ChannelsAttribute(params ChannelType[] channels) : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var current = AcceptanceChannel.Current;
        if (!channels.Contains(current))
            Assert.Skip($"Not applicable to the {current} channel");
    }
}
```

### acceptance.Dsl/Core/Alias.cs

Every test gets its own suffix, so "Buy milk" becomes "Buy milk_3f9a1c". Tests never see each other's data, so there's no database reset and they can run in parallel, even against a system already full of data. Blank names stay blank, so you can still test empty input. `OfLength` gives a unique value of an exact length, suffix included, so the DSL can test length limits without knowing how the suffix is built.

```csharp
namespace acceptance.Core.Dsl;

internal class Alias
{
    private const int SuffixLength = 7;   // "_" plus 6 characters
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..(SuffixLength - 1)];

    public string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name}_{_suffix}";

    // A value unique to this spec that the system sees as exactly `length`
    // characters, suffix included.
    public string OfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, SuffixLength);
        return For(new string('a', length - SuffixLength));
    }
}
```

### acceptance.Drivers/Core/DriverResult.cs

Drivers report what happened instead of throwing. The DSL decides whether that outcome was expected. The `TryCatchAsync` helpers turn an exception a driver expects, such as a Playwright timeout, into a failed result.

```csharp
namespace acceptance.Core.Drivers;

internal record DriverResult(bool IsFailed, string Error = "")
{
    public static readonly DriverResult Okay = new(false);
    public static DriverResult Fail(string error) =>
        new(true, error);

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

### acceptance.Drivers/Core/Wiring/IChannel.cs

The capabilities a channel offers. It is a partial interface with one part per domain, so each domain's members live with that domain.

```csharp
namespace acceptance.Core.Drivers;

// The capabilities one channel offers, split into one part per domain
// (<Domain>/Wiring/IChannel.<Domain>.cs). Required capabilities have no
// default; optional ones default to null, meaning "not supported". Disposing
// a channel closes whatever it opened.
//
// Every part of IChannel, ApiChannel and UiChannel must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
internal partial interface IChannel : IAsyncDisposable;
```

### acceptance.Drivers/Core/Wiring/ApiChannel.cs

This part declares the constructor and opens and closes the `HttpClient`. The domain parts use `http` to create their drivers. Specs run in parallel, so every spec's client shares one connection pool for the whole run, the same way the UI channel shares one browser.

```csharp
namespace acceptance.Core.Drivers;

// Drives the system through its public HTTP API. Every API driver shares
// this channel's HttpClient. Its drivers are wired up one part per domain
// (<Domain>/Wiring/ApiChannel.<Domain>.cs); a capability not wired up
// there is unsupported on this channel and falls back to IChannel's null default.
internal partial class ApiChannel(HttpClient http) : IChannel
{
    // Kept as a field, so the domain parts can use http in their initialisers.
    private readonly HttpClient _http = http;

    // One connection pool for the whole test run, as the UI channel shares
    // one browser: specs run in parallel, and a pool per spec would leave a
    // closed connection behind for every spec. Each spec still gets its own
    // HttpClient, so nothing one spec sets on it reaches another.
    private static readonly SocketsHttpHandler SharedConnections = new();

    public static ApiChannel Open(string baseUrl) =>
        new(new HttpClient(SharedConnections, disposeHandler: false) { BaseAddress = new Uri(baseUrl) });

    // Closes this spec's client. The shared connections stay open for the other specs.
    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
```

### acceptance.Drivers/Core/Wiring/UiChannel.cs

Launching a browser takes about a second, so it happens once per test run, and every UI spec shares that browser. Each spec opens its own *browser context* instead: a private session with its own cookies and storage, which opens in milliseconds and is as isolated as a separate browser. The spec's page lives in that context, and disposing the channel closes it. Every UI driver shares the page, so within one spec they all act in the same session.

The pattern is the same on both channels: what's expensive to start is shared by the whole run, and what holds state belongs to one spec. That's what lets specs run in parallel without slowing each other down or leaking into each other.

```csharp
using Microsoft.Playwright;

namespace acceptance.Core.Drivers;

// Drives the system through the web app in a browser, as a user would.
// Specs run in parallel, so every spec in a test run shares one browser,
// and each spec gets its own browser context: a private session with its
// own cookies and storage, cheap to open and isolated from every other spec.
// Every UI driver shares this channel's page, so within one spec they all
// act in the same session. Its drivers are wired up one part per domain
// (<Domain>/Wiring/UiChannel.<Domain>.cs); a capability not wired up there
// is unsupported on this channel and falls back to IChannel's null default.
internal partial class UiChannel(IPage page, IBrowserContext context) : IChannel
{
    // Kept as a field, so DisposeAsync can close it. The domain parts use
    // page in their initialisers.
    private readonly IBrowserContext _context = context;

    public static async Task<UiChannel> OpenAsync(string baseUrl, TimeSpan timeout)
    {
        var browser = await SharedBrowser.Value;
        var context = await browser.NewContextAsync(new() { BaseURL = baseUrl });
        context.SetDefaultTimeout((float)timeout.TotalMilliseconds);
        var page = await context.NewPageAsync();
        return new UiChannel(page, context);
    }

    // Closes this spec's session. The browser stays open for the other specs.
    public async ValueTask DisposeAsync() => await _context.CloseAsync();

    // Launched by the first UI spec and shared for the rest of the run. Lazy
    // makes sure specs starting at the same time launch it only once. If the
    // launch fails, every UI spec reports that same failure.
    private static readonly Lazy<Task<IBrowser>> SharedBrowser = new(LaunchBrowserAsync);

    private static async Task<IBrowser> LaunchBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();

        // Closed when the test run ends, not after any one spec. The run is
        // over by then, so a failure here has nobody to report to, and the
        // browser closes with the test process anyway.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                browser.CloseAsync().GetAwaiter().GetResult();
                playwright.Dispose();
            }
            catch (Exception) { }
        };
        return browser;
    }
}
```

### acceptance.Dsl/Core/SpecContext.cs

The DSL is organised by **business domain**, not by who is acting: specs say `Todos.Add("Buy milk")`, and later perhaps `Lists.Create(...)` or `Accounts.Register("user: Alice")`. People are data, aliased like everything else, not objects. Each domain's DSL mirrors that domain's drivers, so no single DSL class grows to hold every domain's vocabulary.

Some state belongs to the spec, not to a domain: its aliases, and the outcome of its last action. Every domain's DSL shares this one context.

```csharp
using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// What one spec carries across every domain's DSL: its aliases, and the
// outcome of its last action, whichever domain that action was in.
internal class SpecContext
{
    public Alias Alias { get; } = new();
    public DriverResult? LastResult { get; set; }
}
```

### acceptance.Dsl/Core/DslBase.cs

What every domain's DSL has in common. Each action comes in two forms: `Add` must succeed and fails the test on that line if it doesn't, while `AttemptToAdd` just tries and records the outcome, so the next line can check that the rejection was the right one. `ConfirmErrorIsShown` checks that outcome, whichever domain the action was in. Confirmations are always lines of their own.

```csharp
using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for each business domain's DSL: Todos now, Lists or Accounts later.
// Every domain shares the spec's context, so a spec can act in one domain and
// check the outcome from any of them.
//
// Specs live in another project and see only the public members: the DSL's
// vocabulary. What a DSL is built from (drivers, channels, the spec context)
// is internal, so the constructor and the helpers below are private protected.
public abstract class DslBase
{
    private readonly SpecContext _context;

    private protected DslBase(SpecContext context) => _context = context;

    private protected Alias Alias => _context.Alias;

    private protected void Record(DriverResult result) => _context.LastResult = result;

    // Drivers report what happened; the DSL decides whether that passes.
    private protected void ConfirmLastActionSucceeded(string because)
    {
        Assert.NotNull(_context.LastResult);
        Assert.False(_context.LastResult.IsFailed, $"{because}. The system said: {_context.LastResult.Error}");
    }

    private protected static T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(
            $"This spec uses {description}, which this channel does not support. " +
            "Limit it to the channels that do, e.g. [Fact, Channels(ChannelType.Ui)].");

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_context.LastResult);
        Assert.True(_context.LastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, _context.LastResult.Error);
        return Task.CompletedTask;
    }
}
```

### acceptance.Dsl/Core/Wiring/AcceptanceTestBase.cs

Every spec class inherits from this. It picks the channel for this run, which opens and closes what it drives, and holds the spec's shared context. It knows nothing about any domain: each domain adds its DSL as a part of this partial class, in the same way it adds its drivers to the channels. It also reads how long the UI channel waits for the page, from `ACCEPTANCE_UI_TIMEOUT_SECONDS` (5 seconds by default), so a slow machine is fixed in one place rather than in the drivers.

```csharp
using System.Globalization;
using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for every spec. Each business domain adds its DSL as one part
// (<Domain>/Wiring/AcceptanceTestBase.<Domain>.cs), so specs read like
// Todos.Add("Buy milk"). The specs themselves live in the acceptance.Specs
// project, which sees only what's public here: this class, the DSLs it
// exposes, ChannelType and the Channels attribute. Every part must be declared
// in this namespace, whatever folder it lives in. A part in another namespace
// is silently a different type.
public abstract partial class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    // What each domain's DSL is built from: the channel this run drives,
    // and the context every domain shares within one spec. Private, so specs
    // can only reach the system through the DSL; each domain's part of this
    // class can still use them, because parts of a partial class share members.
    private IChannel? _channel;
    private IChannel Channel => _channel ?? throw new InvalidOperationException("The channel isn't open yet.");
    private SpecContext Context { get; } = new();

    // The channel opens and closes what it drives; this only picks which one.
    public async ValueTask InitializeAsync()
    {
        _channel = AcceptanceChannel.Current switch
        {
            ChannelType.Api => ApiChannel.Open(BaseUrl),
            ChannelType.Ui => await UiChannel.OpenAsync(BaseUrl, UiTimeout),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
    }

    // How long the UI channel waits for the page before giving up. Specs run
    // in parallel, so a busy machine may need longer: set
    // ACCEPTANCE_UI_TIMEOUT_SECONDS rather than changing a driver.
    private static TimeSpan UiTimeout
    {
        get
        {
            const string variable = "ACCEPTANCE_UI_TIMEOUT_SECONDS";
            var value = Environment.GetEnvironmentVariable(variable)?.Trim();
            if (string.IsNullOrEmpty(value)) return TimeSpan.FromSeconds(5);

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : throw new InvalidOperationException($"{variable} must be a number of seconds, not '{value}'.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
    }
}
```

### acceptance.Specs/Core/Wiring/Parallelization.cs

xUnit runs spec classes in parallel, but by default runs the specs inside one class one at a time. This turns that off, so every spec runs alongside every other, and a spec that quietly depends on another one fails straight away. It's an assembly attribute, so it has to live in the specs' project; it sits in `Core/Wiring/` so the harness agent owns it, like the rest of the wiring.

```csharp
// Runs every spec in parallel, including specs in the same class. xUnit's
// default runs each class's specs one at a time, which would hide a spec
// that depends on another spec, or on the state of the whole system.
// How many specs run at once is set per channel, in
// scripts/run-acceptance-tests.ps1. This is harness wiring, owned by the
// harness agent like every project's Core/Wiring: specs don't need it.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.All)]
```

### acceptance.Drivers/Todos/ITodoDriver.cs

The required todo capability: every channel implements it. The domain is in the type name, so method names don't repeat it.

```csharp
using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

// What every channel can do with todos. The domain is in the type name,
// so methods don't repeat it: ITodoDriver.Add, not AddTodo.
internal interface ITodoDriver
{
    Task<DriverResult> Add(string title);
    Task<DriverResult> Complete(string title);

    // Observations: drivers report what they see, and the DSL decides whether
    // that passes. Channels that update asynchronously wait for a yes before
    // answering no.
    Task<bool> IsShown(string title);
    Task<bool> IsComplete(string title);
}
```

### acceptance.Drivers/Todos/ITodoEntryDriver.cs

An optional capability. Only the UI channel supports it. It's named for what the user is doing, not for the control they do it with.

```csharp
namespace acceptance.Todos.Drivers;

// Live feedback while a new todo is being entered, before it is submitted.
// An optional capability: only channels with a live entry form offer it.
internal interface ITodoEntryDriver
{
    Task TypeTitle(string title);
    Task<bool> IsTitleTooLongWarningShown();
}
```

### acceptance.Drivers/Todos/ApiTodoDriver.cs

The comment at the top is the API contract. You've just designed the endpoints; the agent will build them.

```csharp
using System.Net.Http.Json;
using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

// API contract:
//   GET    /api/todos                  -> 200 [{ id, title, completed }]
//   POST   /api/todos                  -> 201, or 400 ValidationProblemDetails when rejected:
//                                         { errors: { "<field>": ["<message>", ...] } },
//                                         e.g. errors.title = ["A title is required"]
//   POST   /api/todos/{id}/complete    -> 2xx

internal class ApiTodoDriver(HttpClient http) : ITodoDriver
{
    public async Task<DriverResult> Add(string title) =>
        await ToResult(await http.PostAsJsonAsync("/api/todos", new { title }));

    public async Task<DriverResult> Complete(string title)
    {
        var todo = await Find(title);
        if (todo is null)
            return DriverResult.Fail($"No todo titled '{title}'");

        return await ToResult(await http.PostAsync($"/api/todos/{todo.Id}/complete", null));
    }

    public async Task<bool> IsShown(string title) =>
        await Find(title) is not null;

    public async Task<bool> IsComplete(string title) =>
        (await Find(title))?.Completed == true;

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

### acceptance.Drivers/Todos/UiTodoDriver.cs

This comment is the UI contract: the `data-testid` attributes Angular must expose. Nothing here depends on CSS classes or layout, so the agent can restyle freely. It's the UI channel's one todo driver, implementing both capabilities. `FillAsync` fires an `input` event, which is what the title input listens to, so there's no need to type character by character.

```csharp
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

// UI contract (data-testid attributes):
//   new-todo-title           text input for a new todo's title
//   add-todo                 submit button; stays enabled, rejections show an error
//   title-too-long-warning   visible while the title being typed is too long,
//                            before the todo is submitted
//   todo-item                one per todo, with data-completed="true" or "false"
//   todo-title               the title text inside a todo-item
//   complete-todo            control inside a todo-item that marks it complete
//   error-message            visible when the last action was rejected

// One driver for every todo capability the browser offers. UiChannel hands
// the same instance out once per capability.
internal class UiTodoDriver(IPage page) : ITodoDriver, ITodoEntryDriver
{
    // ITodoDriver

    public async Task<DriverResult> Add(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
        await page.GetByTestId("add-todo").ClickAsync();
        return await Outcome(LocateTodo(title));
    }

    public async Task<DriverResult> Complete(string title)
    {
        await page.GotoAsync("/");
        await LocateTodo(title).GetByTestId("complete-todo").ClickAsync();
        return await Outcome(LocateCompletedTodo(title));
    }

    public async Task<bool> IsShown(string title)
    {
        await page.GotoAsync("/");
        return await Eventually(LocateTodo(title));
    }

    public async Task<bool> IsComplete(string title)
    {
        await page.GotoAsync("/");
        return await Eventually(LocateCompletedTodo(title));
    }

    // ITodoEntryDriver

    // FillAsync fires an input event, which is what the title input listens to.
    public async Task TypeTitle(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
    }

    public Task<bool> IsTitleTooLongWarningShown() =>
        Eventually(page.GetByTestId("title-too-long-warning"));

    // Helpers

    // Whether the locator becomes visible within the page's default timeout.
    // Waits for a yes, so a no takes the full timeout: to check that something
    // is absent, wait for it to be hidden instead of negating this.
    private static async Task<bool> Eventually(ILocator locator)
    {
        var result = await DriverResult.TryCatchAsync<TimeoutException>(
            () => locator.WaitForAsync());
        return !result.IsFailed;
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

### acceptance.Drivers/Todos/Wiring/IChannel.Todos.cs

The todo domain's part of `IChannel`. A required capability has no default. An optional one defaults to `null`, meaning "not supported", so a channel only names what it can do. The file lives in `Todos/Wiring/`, but its namespace is `acceptance.Core.Drivers`, the same as `Core/Wiring/IChannel.cs`.

```csharp
using acceptance.Todos.Drivers;

// Part of IChannel (see Core/Wiring/IChannel.cs), so it shares that namespace.
namespace acceptance.Core.Drivers;

internal partial interface IChannel
{
    ITodoDriver Todos { get; }
    ITodoEntryDriver? TodoEntry => null;
}
```

### acceptance.Drivers/Todos/Wiring/ApiChannel.Todos.cs

How the API channel provides the todo domain. It doesn't mention `TodoEntry`, so the API channel doesn't support it.

```csharp
using acceptance.Todos.Drivers;

// Part of ApiChannel (see Core/Wiring/ApiChannel.cs), so it shares that namespace.
namespace acceptance.Core.Drivers;

internal partial class ApiChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
```

### acceptance.Drivers/Todos/Wiring/UiChannel.Todos.cs

How the UI channel provides the todo domain. It builds one `UiTodoDriver` and hands it out once per capability, including the optional entry capability.

```csharp
using acceptance.Todos.Drivers;

// Part of UiChannel (see Core/Wiring/UiChannel.cs), so it shares that namespace.
namespace acceptance.Core.Drivers;

// One todo driver for the browser, handed out once per capability it offers.
internal partial class UiChannel
{
    private readonly UiTodoDriver _todos = new(page);

    public ITodoDriver Todos => _todos;
    public ITodoEntryDriver? TodoEntry => _todos;
}
```

### acceptance.Dsl/Todos/TodosDsl.cs

The todo vocabulary. Specs reach it as `Todos`, so method names don't repeat the word. The DSL takes the whole channel and reaches each capability through it. If a spec uses an optional capability on a channel that doesn't have it, the error says how to fix the spec.

```csharp
using acceptance.Core.Dsl;
using acceptance.Core.Drivers;
using acceptance.Todos.Drivers;

namespace acceptance.Todos.Dsl;

// The todo domain's vocabulary: everything a spec can say about todos.
// Specs reach it as Todos, so methods don't repeat the word: Todos.Add("Buy milk").
// Public so specs can use it; built only by AcceptanceTestBase, in this project.
public class TodosDsl : DslBase
{
    private readonly IChannel _channel;

    internal TodosDsl(IChannel channel, SpecContext context) : base(context) => _channel = channel;

    // Drivers are named for the domain capability they offer, never the mechanism.
    private ITodoDriver Todos => _channel.Todos;
    private ITodoEntryDriver Entry =>
        Require(_channel.TodoEntry, "live feedback while entering a todo");

    public async Task AttemptToAdd(string title) =>
        Record(await Todos.Add(Alias.For(title)));

    public async Task AttemptToAddWithTitleOfLength(int length) =>
        Record(await Todos.Add(Alias.OfLength(length)));

    public async Task Add(string title)
    {
        await AttemptToAdd(title);
        ConfirmLastActionSucceeded($"Could not add todo '{title}'");
    }

    public async Task AttemptToComplete(string title) =>
        Record(await Todos.Complete(Alias.For(title)));

    public async Task Complete(string title)
    {
        await AttemptToComplete(title);
        ConfirmLastActionSucceeded($"Could not complete todo '{title}'");
    }

    public async Task ConfirmShown(string title) =>
        Assert.True(await Todos.IsShown(Alias.For(title)),
            $"Expected todo '{title}' to be shown");

    public async Task ConfirmComplete(string title) =>
        Assert.True(await Todos.IsComplete(Alias.For(title)),
            $"Expected todo '{title}' to be complete");

    // Live feedback while entering a todo

    public Task TypeTitleOfLength(int length) =>
        Entry.TypeTitle(Alias.OfLength(length));

    public async Task ConfirmWarnedTitleIsTooLong() =>
        Assert.True(await Entry.IsTitleTooLongWarningShown(),
            "Expected a warning that the title is too long");
}
```

### acceptance.Dsl/Todos/Wiring/AcceptanceTestBase.Todos.cs

The todo domain's part of `AcceptanceTestBase`: the `Todos` property specs use. It lives in `Wiring/` because, like the channel parts in the drivers, it connects this layer to the one above: here, the todo DSL to the specs. It stays in `acceptance.Dsl` rather than moving to `acceptance.Specs`, because a partial class can't span two projects and the base needs the internal channels to build the DSL. A new domain adds one file like this, in its own folder, and nothing in `Core/` changes.

```csharp
using acceptance.Todos.Dsl;

// Part of AcceptanceTestBase (see Core/Wiring/AcceptanceTestBase.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public abstract partial class AcceptanceTestBase
{
    protected TodosDsl Todos => field ??= new TodosDsl(Channel, Context);
}
```

Run `dotnet build acceptance.Specs`, which builds all three projects. It should compile with no tests yet. Commit.

## Step 5: Roles and guardrails

The agent must never be able to make a spec pass by changing the spec, a driver or the checks themselves. And the agent that helps you *write* a spec should never be the one that implements it: if it heard your design conversation, it would fill gaps in the spec from memory, and the spec would stop being the whole contract.

So every Claude Code session has one **role**:

| Role | May change | Typical work |
| --- | --- | --- |
| **Spec agent** (`spec-agent`) | `acceptance.Specs/`, `acceptance.Dsl/` and `acceptance.Drivers/`, except the harness plumbing | Write specs with you, grow the DSL, design driver contracts, watch specs fail |
| **App agent** (`app-agent`) | `api/`, `api.UnitTests/`, `api.IntegrationTests/`, `web/` | Make a committed, failing spec pass |
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
- The drivers in `acceptance.Drivers/<Domain>` define the contract: API routes,
  JSON shape and the UI's `data-testid` attributes.
- Specs describe behaviour in domain language. Every spec must pass on
  every channel unless it is marked `[Fact, Channels(...)]`.
- Specs run in parallel against one shared, running system. A spec checks
  only what it created itself, through the DSL's aliased names, never the
  state of the whole system, such as how many todos there are in total.
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
        "acceptance.Specs/",
        "acceptance.Dsl/",
        "acceptance.Drivers/"
      ]
    },
    "app-agent": {
      "summary": "Makes a committed, failing spec pass. Never touches specs or the harness.",
      "worksOnTemplate": false,
      "owns": [
        "api/",
        "api.UnitTests/",
        "api.IntegrationTests/",
        "web/"
      ]
    },
    "harness-agent": {
      "summary": "Improves the harness and the template. Writes no specs or app code.",
      "worksOnTemplate": true,
      "owns": [
        "acceptance.Dsl/Core/Wiring/",
        "acceptance.Drivers/Core/Wiring/",
        "acceptance.Specs/Core/Wiring/",
        "acceptance.Specs/Todo.Acceptance.Specs.csproj",
        "acceptance.Dsl/Todo.Acceptance.Dsl.csproj",
        "acceptance.Drivers/Todo.Acceptance.Drivers.csproj",
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
        "Edit(/.claude/hooks/check-protected-files.ps1)"
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

- Work in the spec layers: one project per layer, one folder per domain
  in each. Specs go in `acceptance.Specs/<Domain>`, the vocabulary in
  `acceptance.Dsl/<Domain>` and the contract in
  `acceptance.Drivers/<Domain>`, with what every domain shares in each
  project's `Core` (except each `Core/Wiring`, which the harness agent
  owns). A domain plugs into the layer above through its `Wiring` folder:
  its parts of the channels in `acceptance.Drivers`, its part of
  `AcceptanceTestBase` in `acceptance.Dsl`. Specs can only see the DSL,
  and only the DSL can see the drivers. Keep
  each layer to its own job: specs say what, the DSL gives the words,
  drivers say how each channel reaches the system.
- Read the app code only to understand current behaviour. Never shape a
  spec around how the code happens to work.
- When a spec needs new DSL methods or driver contract (endpoints, status
  codes, `data-testid` values), propose them and explain the decisions
  they force. Ask about anything the human hasn't decided.
- Prefer specs for every channel. Use `[Fact, Channels(...)]` only for
  behaviour a channel genuinely lacks, and split a business rule from its
  channel-specific feedback.
- Check your work: `dotnet build acceptance.Specs` must pass, and
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
- API unit tests live in `api.UnitTests/` (`Todo.Api.UnitTests`); web
  tests use the Angular project's own setup.
- Add narrow integration tests (one component, one real dependency) for
  persistence against a real database, framework configuration and
  external adapters. They live in `api.IntegrationTests/`
  (`Todo.Api.IntegrationTests`), apart from the unit tests so those stay
  fast to run. Create that project with your first integration test.
  Never re-test what acceptance specs cover.
- .NET test projects use the same xUnit v3 packages and versions as
  `acceptance.Specs/Todo.Acceptance.Specs.csproj` (`xunit.v3.mtp-off`
  and `xunit.runner.visualstudio`). v3 test projects are executables, so
  set `<OutputType>Exe</OutputType>`.
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
that `pwsh -File .claude/hooks/check-protected-files.ps1` passes. If your latest
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
4. Implement in small steps, following the testing rules in your role instructions,
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
| `scripts/run-acceptance-tests.ps1` | Starts Docker if needed, deploys fresh containers, runs every spec on each channel, or on one with `-Channel ui` |
| `scripts/check-ready-to-implement.ps1` | Checks the tree is clean and the named spec is committed |
| `scripts/check-done.ps1` | The definition of done: the protected-files check, all unit tests, the API integration tests once there are any, and all acceptance tests |

The ones you run most also have commands in the Claude Code chat, so they tab-complete: `/run-specs [channel]`, `/check-done` and `/check-ready <SpecName>`. Claude runs the script and summarises the result. The commands are small files in `.claude/commands/`; the scripts still work on their own in a terminal.

The role machinery lives beside the settings that wire it up. You don't run it directly:

| Script | What it does |
| --- | --- |
| `.claude/hooks/role-hooks.ps1` | The four hooks that enforce the session's role |
| `.claude/hooks/roles.ps1` | Shared by the hooks and the role scripts: reads `roles.json`, works out who owns a path, tracks what changed |
| `.claude/hooks/check-protected-files.ps1` | Fails if an uncommitted change is outside the role of the last session started here; `check-done.ps1` runs it |

The channel names are listed twice, in the `ChannelType` enum and in `run-acceptance-tests.ps1`, and each points to the other: add or rename a channel in both. `run-acceptance-tests.ps1` uses `--force-recreate`, so every run gets fresh containers and data doesn't build up across runs. Within a run both channels share one system, which is fine because aliasing keeps specs apart.

Specs run in parallel, against that one system. Running them in parallel by default makes the suite faster, but the main reason is that it keeps the specs honest: a spec that depends on what other specs did, or on the whole system being in a particular state, fails now, while it's one spec, rather than after hundreds of specs have come to depend on running one at a time. The script sets how many specs run at once on each channel, in `$parallelSpecs` at the top: wide for the API, narrower for the UI, where each spec drives a real page. If UI specs time out only when the whole suite runs, lower the UI number or raise `ACCEPTANCE_UI_TIMEOUT_SECONDS`.

If Docker isn't running, it starts Docker Desktop and waits; to avoid the wait, turn on *Start Docker Desktop when you sign in* in Docker Desktop's general settings.

To run one channel by itself, for example while writing a spec, start the stack and set the channel for that run:

```powershell
docker compose up -d --build
$env:ACCEPTANCE_CHANNEL = "ui"; dotnet test acceptance.Specs
```

On a real team the same checks run in CI, with a `CODEOWNERS` entry putting each role's files behind the right reviewers. Commit all of these files before starting the next step.

## Step 6: First loop, add a todo

This is the loop you'll repeat for every feature: write a spec, watch it fail, commit, hand it over, review.

**1. Write the spec** in `acceptance.Specs/Todos/AddingTodos.cs`. Write this first one yourself. Later, a spec agent can help: type `/spec-agent`, then `/clear`, and talk the feature through.

```csharp
using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

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

The DSL and drivers already support completing, so this one is just a spec. Create `acceptance.Specs/Todos/CompletingTodos.cs`:

```csharp
using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

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
[Fact, Channels(ChannelType.Ui)]
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

This is the payoff: big implementation changes with zero changes to the acceptance projects. Try at least the first two.

1. **Swap the storage.** Ask the agent to replace in-memory storage with EF Core and SQLite, persisted in a Docker volume. Every spec should still pass untouched.
2. **Restyle the UI.** Ask for a full visual redesign or a component library. The drivers rely only on test IDs, so the specs survive.
3. **Test the lock.** Write a spec the app can't reasonably satisfy, such as an assertion with a typo in the expected message, and ask the agent to "make the build green". It should stop and flag the spec rather than edit it. If it tries anyway, the role hooks will block or catch it. Then try the opposite with a spec agent: ask it to implement the feature its spec describes.

4. **Check the whole system, then don't.** Write a spec that checks something about every todo, such as how many are shown, or that only the todos it added are there. It may pass when you run it alone, but it fails in the full suite, because other specs are adding todos to the same system at the same time. Then rewrite it to check only what it created: "given I add three todos, when I complete two of them, then only one of *those* is still to do". Every todo app has other people's data in it; a spec that only talks about its own data is true however much else is there.

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
| A spec passes alone but fails in the full suite | It depends on other specs, or checks the whole system; make it check only what it created |
| UI specs time out only when the whole suite runs | The machine is overloaded; lower `ui` in `$parallelSpecs` in `run-acceptance-tests.ps1`, or raise `ACCEPTANCE_UI_TIMEOUT_SECONDS` |
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
