namespace acceptance.Core.Drivers;

// Drives the system through its public HTTP API. Every API driver shares
// this channel's HttpClient. Its drivers are wired up one part per domain
// (<Domain>/Wiring/ApiChannel.<Domain>.cs); a capability not wired up
// there is unsupported on this channel and falls back to IChannel's null default.
internal partial class ApiChannel(HttpClient http) : IChannel
{
    // Kept as a field, so the domain parts can use http in their initialisers.
    private readonly HttpClient _http = http;

    // One connection pool for the whole test run, as the UI channel shares
    // one browser: specs run in parallel, and a pool per spec would leave a
    // closed connection behind for every spec. Each spec still gets its own
    // HttpClient, so nothing one spec sets on it reaches another.
    private static readonly SocketsHttpHandler SharedConnections = new();

    public static ApiChannel Open(string baseUrl) =>
        new(new HttpClient(SharedConnections, disposeHandler: false) { BaseAddress = new Uri(baseUrl) });

    // Closes this spec's client. The shared connections stay open for the other specs.
    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
