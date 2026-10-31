using Microsoft.Playwright;
using acceptance.Core.Spec;
using acceptance.Core.Spec.Channels;

namespace acceptance.Core.Harness;

// The base for every spec. Each business domain adds its DSL as one part
// (<Domain>/Dsl/AcceptanceTestBase.<Domain>.cs), so specs read like
// Todos.Add("Buy milk"). Every part must be declared in this namespace,
// whatever folder it lives in. A part in another namespace is silently a
// different type.
public abstract partial class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    // What each domain's DSL is built from: the channel this run drives,
    // and the context every domain shares within one spec.
    protected IChannel Channel { get; private set; } = null!;
    protected SpecContext Context { get; } = new();

    public async ValueTask InitializeAsync()
    {
        Channel = AcceptanceChannel.Current switch
        {
            ChannelType.Api => CreateApiChannel(),
            ChannelType.Ui => await CreateUiChannel(),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
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
