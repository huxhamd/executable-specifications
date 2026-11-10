using Microsoft.Playwright;

namespace acceptance.Core.Drivers;

internal partial class UiChannel(IPage page, IBrowserContext context) : IChannel
{
    private readonly IBrowserContext _context = context;

    public static async Task<UiChannel> OpenAsync(string baseUrl, TimeSpan timeout)
    {
        var browser = await SharedBrowser.Value;
        var context = await browser.NewContextAsync(new() { BaseURL = baseUrl });
        context.SetDefaultTimeout((float)timeout.TotalMilliseconds);
        var page = await context.NewPageAsync();
        return new UiChannel(page, context);
    }

    public async ValueTask DisposeAsync() => await _context.CloseAsync();

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
