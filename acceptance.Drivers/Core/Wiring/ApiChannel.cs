namespace acceptance.Core.Drivers;

internal partial class ApiChannel(HttpClient http) : IChannel
{
    private readonly HttpClient _http = http;

    // One connection pool for the whole test run, as the UI channel shares
    // one browser: specs run in parallel, and a pool per spec would leave a
    // closed connection behind for every spec. Each spec still gets its own
    // HttpClient, so nothing one spec sets on it reaches another.
    private static readonly SocketsHttpHandler SharedConnections = new();

    public static ApiChannel Open(string baseUrl) =>
        new(new HttpClient(SharedConnections, disposeHandler: false) { BaseAddress = new Uri(baseUrl) });

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
