namespace acceptance.Drivers.Channels;

// Drives the system through its public HTTP API. Every API driver shares
// this channel's HttpClient. Its drivers are wired up one part per domain
// (Drivers/<Domain>/Api/ApiChannel.<Domain>.cs); a capability not wired up
// there is unsupported on this channel and falls back to IChannel's null default.
public partial class ApiChannel(HttpClient http) : IChannel;
