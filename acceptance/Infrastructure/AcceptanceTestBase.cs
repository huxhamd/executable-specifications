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
