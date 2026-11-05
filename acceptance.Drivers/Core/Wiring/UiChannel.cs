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
