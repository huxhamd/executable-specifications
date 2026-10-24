using Microsoft.Playwright;
using acceptance.Dsl;
using acceptance.Drivers;

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
        var channel = Environment
            .GetEnvironmentVariable("ACCEPTANCE_CHANNEL")
            ?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(channel)) throw new InvalidOperationException(
            "Set ACCEPTANCE_CHANNEL to 'api' or 'ui' before running the acceptance tests.");
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