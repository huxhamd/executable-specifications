using Microsoft.Playwright;
using acceptance.Core.Dsl;
using acceptance.Core.Channels;

namespace acceptance.Core.Specs;

// The base for every spec. Each business domain adds its DSL as one part,
// next to its specs (<Domain>/Specs/AcceptanceTestBase.<Domain>.cs), so specs read like
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
    // and the context every domain shares within one spec. Private, so specs
    // can only reach the system through the DSL; each domain's part of this
    // class can still use them, because parts of a partial class share members.
    private IChannel Channel { get; set; } = null!;
    private SpecContext Context { get; } = new();

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
