using Microsoft.Playwright;

namespace acceptance.Core.Channels;

// Drives the system through the web app in a browser, as a user would.
// Every UI driver shares this channel's page, so they all act in one browser
// session. Its drivers are wired up one part per domain
// (<Domain>/Channels/UiChannel.<Domain>.cs); a capability not wired up there
// is unsupported on this channel and falls back to IChannel's null default.
public partial class UiChannel(IPage page) : IChannel;
