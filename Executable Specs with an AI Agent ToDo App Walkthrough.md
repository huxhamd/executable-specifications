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
mkdir todo-demo && cd todo-demo
git init
dotnet new gitignore

# API: the stock template, the agent will replace the weather endpoint
dotnet new webapi -n Todo.Api -o api

# Angular app: no SSR, no nested git repo
npx @angular/cli@latest new web --skip-git --ssr=false --style=css --routing=false

# Acceptance tests: xUnit plus Playwright
dotnet new xunit -n Todo.AcceptanceTests -o acceptance
cd acceptance
dotnet add package Microsoft.Playwright
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
cd ..
```

Delete the template's `UnitTest1.cs` from `acceptance/`, then commit: `git add -A && git commit -m "Scaffold"`.

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
name: todo-demo

services:
  api:
    build: ./api
    image: todo-demo-api
    container_name: todo-demo-api
  web:
    build: ./web
    image: todo-demo-web
    container_name: todo-demo-web
    ports: ["4200:80"]
    depends_on: [api]
```

Add a `.dockerignore` in `api/` containing `bin/` and `obj/`, and one in `web/` containing `node_modules/`, `dist/` and `.angular/`.

Check it works: `docker compose up -d --build`, then open `http://localhost:4200`. You should see the Angular welcome page. Commit.

## Step 4: Build the test plumbing

This is the one-off setup cost. Type these files in yourself rather than pasting, because the point is to understand each layer. Namespaces are left out to keep them short.

```
acceptance/
  AcceptanceTestBase.cs     picks a driver, builds the DSL
  Dsl/Alias.cs        unique names per test
  Dsl/UserDsl.cs        the vocabulary specs use
  Drivers/DriverResult.cs
  Drivers/ITodoDriver.cs
  Drivers/ApiTodoDriver.cs
  Drivers/UiTodoDriver.cs
  Specs/                empty for now
```

### Dsl/Alias.cs

Every test gets its own suffix, so "Buy milk" becomes "Buy milk 3f9a1c". Tests never see each other's data, so there's no database reset and they can run in parallel. Blank names stay blank, so you can still test empty input.

```csharp
public class Alias
{
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..6];

    public string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name} {_suffix}";
}
```

### Drivers/DriverResult.cs

Drivers report what happened instead of throwing. The DSL decides whether that outcome was expected.

```csharp
using Xunit;

public record DriverResult(bool IsSuccess, string Error = "")
{
    public bool IsFailure => !IsSuccess;

    public static readonly DriverResult Ok = new(true);
    public static DriverResult Failed(string error) => new(false, error);

    public void ShouldSucceed(string because) =>
        Assert.True(IsSuccess, $"{because}. The system said: {Error}");
}
```

### Drivers/ITodoDriver.cs

```csharp
public interface ITodoDriver
{
    Task<DriverResult> AddTodo(string title);
    Task<DriverResult> CompleteTodo(string title);
    Task ConfirmTodoIsShown(string title);
    Task ConfirmTodoIsComplete(string title);
}
```

### Drivers/ApiTodoDriver.cs

The comment at the top is the API contract. You've just designed the endpoints; the agent will build them.

```csharp
using System.Net.Http.Json;
using Xunit;

// Contract:
//   GET  /api/todos                -> 200 [{ id, title, completed }]
//   POST /api/todos  { title }     -> 201, or 400 with a message when rejected
//   POST /api/todos/{id}/complete  -> 2xx
public class ApiTodoDriver(HttpClient http) : ITodoDriver
{
    public async Task<DriverResult> AddTodo(string title) =>
        await ToResult(await http.PostAsJsonAsync("/api/todos", new { title }));

    public async Task<DriverResult> CompleteTodo(string title)
    {
        var todo = await Find(title);
        if (todo is null) return DriverResult.Failed($"No todo titled '{title}'");
        return await ToResult(await http.PostAsync($"/api/todos/{todo.Id}/complete", null));
    }

    public async Task ConfirmTodoIsShown(string title) =>
        Assert.True(await Find(title) is not null, $"Expected '{title}' in GET /api/todos");

    public async Task ConfirmTodoIsComplete(string title) =>
        Assert.True((await Find(title))?.Completed == true, $"Expected '{title}' to be completed");

    private async Task<TodoDto?> Find(string title) =>
        (await http.GetFromJsonAsync<List<TodoDto>>("/api/todos"))!
            .FirstOrDefault(t => t.Title == title);

    private static async Task<DriverResult> ToResult(HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? DriverResult.Ok
            : DriverResult.Failed(await response.Content.ReadAsStringAsync());

    private record TodoDto(int Id, string Title, bool Completed);
}
```

### Drivers/UiTodoDriver.cs

This comment is the UI contract: the `data-testid` attributes Angular must expose. Nothing here depends on CSS classes or layout, so the agent can restyle freely.

```csharp
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

// Contract (data-testid attributes):
//   new-todo-title  text input for a new todo's title
//   add-todo        submit button; stays enabled, rejections show an error
//   todo-item       one per todo, with data-completed="true" or "false"
//   todo-title      the title text inside a todo-item
//   complete-todo   control inside a todo-item that marks it complete
//   error-message   visible when the last action was rejected
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

    // Exact title match, so "" only matches a todo whose title really is blank.
    private ILocator LocateTodo(string title) =>
        page.GetByTestId("todo-item").Filter(new()
        {
            Has = page.GetByTestId("todo-title").Filter(new()
            {
                HasTextRegex = new Regex($"^{Regex.Escape(title)}$")
            })
        });

    private ILocator LocateCompletedTodo(string title) =>
        LocateTodo(title).And(page.Locator("[data-completed='true']"));

    // Wait for whichever comes first: the expected change, or an error.
    private async Task<DriverResult> Outcome(ILocator success)
    {
        var error = page.GetByTestId("error-message");
        try
        {
            await success.Or(error).First.WaitForAsync();
        }
        catch (TimeoutException)
        {
            return DriverResult.Failed(
                "Nothing happened: neither the expected change nor an error appeared");
        }
        return await error.IsVisibleAsync()
            ? DriverResult.Failed(await error.InnerTextAsync())
            : DriverResult.Ok;
    }
}
```

### Dsl/UserDsl.cs

Each action comes in two forms. `AddTodo` must succeed and fails the test on that line if it doesn't. `AttemptToAddTodo` just tries and records the outcome, so the next line can check that the rejection was the right one. Confirmations are always lines of their own.

```csharp
using Xunit;

public class UserDsl(ITodoDriver driver, Alias alias)
{
    private DriverResult? _last;

    public async Task AttemptToAddTodo(string title) =>
        _last = await driver.AddTodo(alias.For(title));

    public async Task AddTodo(string title)
    {
        await AttemptToAddTodo(title);
        _last!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToCompleteTodo(string title) =>
        _last = await driver.CompleteTodo(alias.For(title));

    public async Task CompleteTodo(string title)
    {
        await AttemptToCompleteTodo(title);
        _last!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmTodoIsShown(string title) => driver.ConfirmTodoIsShown(alias.For(title));

    public Task ConfirmTodoIsComplete(string title) => driver.ConfirmTodoIsComplete(alias.For(title));

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_last);
        Assert.True(_last.IsFailure, "Expected the last action to be rejected, but it succeeded");
        Assert.Contains(message, _last.Error, StringComparison.OrdinalIgnoreCase);
        return Task.CompletedTask;
    }
}
```

### AcceptanceTestBase.cs

The `ACCEPTANCE_CHANNEL` environment variable chooses the channel, so every spec runs unchanged through the API or the browser.

```csharp
using Microsoft.Playwright;
using Xunit;

public abstract class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    protected UserDsl User { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var channel = GetDriverChannel();
        ITodoDriver driver = channel switch
        {
            "api" => CreateApiDriver(),
            "ui" => await CreateUiDriver(),
            _ => throw new InvalidOperationException(
                $"Unknown ACCEPTANCE_CHANNEL '{channel}'. Use 'api' or 'ui'.")
        };
        User = new UserDsl(driver, new Alias());
    }

    private static string GetDriverChannel()
    {
        var channel = Environment.GetEnvironmentVariable("ACCEPTANCE_CHANNEL")?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(channel))
            throw new InvalidOperationException("Set ACCEPTANCE_CHANNEL to 'api' or 'ui' before running the acceptance tests.");
        return channel;
    }

    private static ITodoDriver CreateApiDriver() =>
        new ApiTodoDriver(new HttpClient { BaseAddress = new Uri(BaseUrl) });

    private async Task<ITodoDriver> CreateUiDriver()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        var page = await _browser.NewPageAsync(new() { BaseURL = BaseUrl });
        page.SetDefaultTimeout(5_000);
        return new UiTodoDriver(page);
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
```

If your template generated xUnit v3 (check the package names in the `.csproj`), `IAsyncLifetime` uses `ValueTask` instead: change both method signatures to `public async ValueTask InitializeAsync()` and `public async ValueTask DisposeAsync()`.

Run `dotnet build acceptance`. It should compile with no tests yet. Commit.

## Step 5: Lock the specs

Use three layers, because each one alone has a gap. The instructions tell the agent the rules, the permission rule blocks its file-edit tools, and the git check catches anything that slips through a shell command.

**`CLAUDE.md`** (repo root): the working agreement

```markdown
# Working agreement

- `acceptance/` holds executable specifications owned by the human.
  Never edit, delete, skip or rename anything in it. If a spec looks
  wrong, stop and explain why instead of working around it.
- The drivers in `acceptance/Drivers` define the contract: API routes
  and JSON shape, and the `data-testid` attributes the UI must expose.
- Test-drive internal code with unit tests: xUnit for the API (add a
  `Todo.Api.Tests` project), the Angular project's own test setup for web.
- A task is done when all of these pass:
  1. `docker compose up -d --build`
  2. `ACCEPTANCE_CHANNEL=api dotnet test acceptance`
  3. `ACCEPTANCE_CHANNEL=ui dotnet test acceptance`
  4. all unit tests
```

**`.claude/settings.json`**: blocks the agent's edit tools on the folder

```json
{
  "permissions": {
    "deny": ["Edit(/acceptance/**)"]
  }
}
```

The leading `/` anchors the path to the project root. Edit rules cover all of Claude Code's file-editing tools, but not a shell command, which is why the next check matters.

**The real lock: a git check you run after every agent session**

```bash
git status --porcelain acceptance/   # must print nothing
```

Always commit your specs *before* handing work to the agent, so this check has a clean baseline. On a real team this becomes a CI step plus a `CODEOWNERS` entry; locally the habit is enough.

On Windows PowerShell, set the channel with `$env:ACCEPTANCE_CHANNEL="ui"; dotnet test acceptance` instead of the `ACCEPTANCE_CHANNEL=ui` prefix. Commit both files.

## Step 6: First loop, add a todo

This is the loop you'll repeat for every feature: write a spec, watch it fail, commit, hand it over, review.

**1. Write the spec** in `acceptance/Specs/AddingTodos.cs`:

```csharp
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

**2. Watch it fail, for the right reason.** With the stack running, run both channels:

```bash
docker compose up -d --build
ACCEPTANCE_CHANNEL=api dotnet test acceptance
ACCEPTANCE_CHANNEL=ui dotnet test acceptance
```

The API run should fail at `AddTodo` with "Could not add todo 'Buy milk'" and a 404 from the API. The UI run should time out looking for `new-todo-title`. If it fails anywhere else, your plumbing has a bug; fix that first.

**3. Commit the spec.** `git add -A && git commit -m "Spec: user can add a todo"`

**4. Hand it to the agent.** Start `claude` in the repo root and give it something like:

```text
Make the failing acceptance spec UserCanAddATodo pass with both
ACCEPTANCE_CHANNEL=api and ACCEPTANCE_CHANNEL=ui. Read CLAUDE.md first. The contract is in the
comments at the top of the two driver files. Keep it simple: in-memory
storage is fine for now. Do not modify anything under acceptance/.
```

**5. Review.**

- [ ] `git status --porcelain acceptance/` prints nothing
- [ ] Both channels pass when you run them yourself, not just when the agent says so
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
    await User.ConfirmErrorIsShown("title is required");
}
```

Run both channels. Right now the app probably accepts blank titles, so expect "Expected the last action to be rejected, but it succeeded" on both. In the UI run, the driver spotted a todo with an empty title appearing. That's the exact-match locator doing its job.

Note that `"title is required"` pins the wording on purpose. The API must return it in the 400 body and the UI must show it in `error-message`. If you'd rather not fix the wording, make the DSL method check only that *some* error was shown.

Commit, then hand it over:

```text
Make CannotAddATodoWithoutATitle pass on both channels without
breaking UserCanAddATodo. Validate in the API; the UI may also
validate, but must show the error in error-message.
```

Run the same review checklist as step 6.

## Step 8: Complete a todo, then design one yourself

The DSL and drivers already support completing, so this one is just a spec. Create `acceptance/Specs/CompletingTodos.cs`:

```csharp
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

This is where most of the real thinking lives. You'll notice that writing the driver forces decisions a prose spec would leave vague.

## Step 9: Prove the point

This is the payoff: big implementation changes with zero changes to `acceptance/`. Try at least the first two.

1. **Swap the storage.** Ask the agent to replace in-memory storage with EF Core and SQLite, persisted in a Docker volume. Every spec should still pass untouched.
2. **Restyle the UI.** Ask for a full visual redesign or a component library. The drivers rely only on test IDs, so the specs survive.
3. **Test the lock.** Write a spec the app can't reasonably satisfy, such as an assertion with a typo in the expected message, and ask the agent to "make the build green". It should stop and flag the spec rather than edit it. If it tries anyway, the permission rule or your git check will catch it.

If a spec breaks during 1 or 2, look at why. It usually means the spec or driver leaked an implementation detail, which is exactly the feedback this approach is meant to give you.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| Every API call returns 502 | The API container isn't up yet; wait a few seconds, or check `docker compose logs api` |
| UI driver times out at the add button | The agent disabled the button for blank input; the contract says it stays enabled |
| UI tests pass but the API channel fails | Logic lives only in Angular; validation must also be in the API |
| Playwright says no browser is installed | Re-run the `playwright.ps1 install chromium` step after any package upgrade |
| Old behaviour after the agent's changes | You forgot `--build` on `docker compose up` |

## Questions to reflect on

- How much of your time went on specs and drivers, versus reviewing the agent's code?
- Where did writing a driver force a decision you'd otherwise have left vague?
- Which review checks would you trust enough to automate, and which need a human?
