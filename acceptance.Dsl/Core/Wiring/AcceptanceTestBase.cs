using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for every spec. Each business domain adds its DSL as one part
// (<Domain>/Wiring/AcceptanceTestBase.<Domain>.cs), so specs read like
// Todos.Add("Buy milk"). The specs themselves live in the acceptance.Specs
// project, which sees only what's public here: this class, the DSLs it
// exposes, ChannelType and the Channels attribute. Every part must be declared
// in this namespace, whatever folder it lives in. A part in another namespace
// is silently a different type.
public abstract partial class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    // What each domain's DSL is built from: the channel this run drives,
    // and the context every domain shares within one spec. Private, so specs
    // can only reach the system through the DSL; each domain's part of this
    // class can still use them, because parts of a partial class share members.
    private IChannel? _channel;
    private IChannel Channel => _channel ?? throw new InvalidOperationException("The channel isn't open yet.");
    private SpecContext Context { get; } = new();

    // The channel opens and closes what it drives; this only picks which one.
    public async ValueTask InitializeAsync()
    {
        _channel = AcceptanceChannel.Current switch
        {
            ChannelType.Api => ApiChannel.Open(BaseUrl),
            ChannelType.Ui => await UiChannel.OpenAsync(BaseUrl),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
    }
}
