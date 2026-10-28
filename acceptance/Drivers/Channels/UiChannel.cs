using Microsoft.Playwright;

namespace acceptance.Drivers.Channels;

// Drives the system through the web app in a browser, as a user would.
// Every UI driver shares this channel's page, so they all act in one browser
// session. Its drivers are wired up one part per domain
// (Drivers/<Domain>/Ui/UiChannel.<Domain>.cs); a capability not wired up there
// is unsupported on this channel and falls back to IChannel's null default.
public partial class UiChannel(IPage page) : IChannel;
