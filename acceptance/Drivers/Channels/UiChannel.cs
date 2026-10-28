using Microsoft.Playwright;

namespace acceptance.Drivers.Channels;

#pragma warning disable CS9113 // Parameter is unread.
public partial class UiChannel(IPage page) : IChannel { }
#pragma warning restore CS9113 // Parameter is unread.