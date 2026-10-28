// Partial class for the API channel, providing capabilities for interacting with the API domain.
namespace acceptance.Drivers.Channels;

// Drives the system through its public HTTP API. Every API driver shares
// this channel's HttpClient. Its drivers are wired up one part per domain
// (Drivers/<Domain>/ApiChannel.<Domain>.cs); a capability not wired up there
// is unsupported on this channel and falls back to IChannel's null default.
#pragma warning disable CS9113 // Parameter is unread.
public partial class ApiChannel(HttpClient http) : IChannel { }
#pragma warning restore CS9113 // Parameter is unread.