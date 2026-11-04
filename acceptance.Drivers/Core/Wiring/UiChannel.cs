using Microsoft.Playwright;

namespace acceptance.Core.Drivers;

// Drives the system through the web app in a browser, as a user would.
// Every UI driver shares this channel's page, so they all act in one browser
// session. Its drivers are wired up one part per domain
// (<Domain>/Wiring/UiChannel.<Domain>.cs); a capability not wired up there
// is unsupported on this channel and falls back to IChannel's null default.
internal partial class UiChannel(IPage page, IBrowser browser, IPlaywright playwright) : IChannel
{
    // Kept as fields, so the domain parts can use page in their initialisers.
    private readonly IBrowser _browser = browser;
    private readonly IPlaywright _playwright = playwright;

    public static async Task<UiChannel> OpenAsync(string baseUrl)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync(new() { BaseURL = baseUrl });
        page.SetDefaultTimeout(5_000);
        return new UiChannel(page, browser, playwright);
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
