namespace acceptance.Core.Drivers;

// Drives the system through its public HTTP API. Every API driver shares
// this channel's HttpClient. Its drivers are wired up one part per domain
// (<Domain>/Wiring/ApiChannel.<Domain>.cs); a capability not wired up
// there is unsupported on this channel and falls back to IChannel's null default.
internal partial class ApiChannel(HttpClient http) : IChannel
{
    // Kept as a field, so the domain parts can use http in their initialisers.
    private readonly HttpClient _http = http;

    public static ApiChannel Open(string baseUrl) =>
        new(new HttpClient { BaseAddress = new Uri(baseUrl) });

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
