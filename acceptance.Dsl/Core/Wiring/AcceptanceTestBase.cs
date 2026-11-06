using System.Globalization;
using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for every spec. Each business domain adds its DSL as one part
// (<Domain>/Wiring/AcceptanceTestBase.<Domain>.cs), so specs read like
// Todos.Add("Buy milk"), and adds its setup vocabulary to Given, so specs
// arrange what they need with Given.Todos.Add("Buy milk"). The specs
// themselves live in the acceptance.Specs project, which sees only what's
// public here: this class, the DSLs it exposes, Given, ChannelType and the
// Channels attribute. Every part must be declared
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

    // What a spec arranges before it acts goes through the API on every run:
    // it's the fastest way in that the real system supports, and the journey
    // through each channel is the business of the specs that test it. On the
    // API run that's the channel already open. On other runs a second
    // channel opens the first time a spec uses Given, and specs that don't
    // use it never open one. Given shares Context with the DSLs above, so
    // "Buy milk" means the same todo whichever channel set it up.
    private IChannel? _setupChannel;
    private IChannel SetupChannel => _setupChannel ??= Channel as ApiChannel ?? ApiChannel.Open(BaseUrl);

    protected GivenDsl Given => field ??= new GivenDsl(SetupChannel, Context);

    // The channel opens and closes what it drives; this only picks which one.
    public async ValueTask InitializeAsync()
    {
        _channel = AcceptanceChannel.Current switch
        {
            ChannelType.Api => ApiChannel.Open(BaseUrl),
            ChannelType.Ui => await UiChannel.OpenAsync(BaseUrl, UiTimeout),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
    }

    // How long the UI channel waits for the page before giving up. Specs run
    // in parallel, so a busy machine may need longer: set
    // ACCEPTANCE_UI_TIMEOUT_SECONDS rather than changing a driver.
    private static TimeSpan UiTimeout
    {
        get
        {
            const string variable = "ACCEPTANCE_UI_TIMEOUT_SECONDS";
            var value = Environment.GetEnvironmentVariable(variable)?.Trim();
            if (string.IsNullOrEmpty(value)) return TimeSpan.FromSeconds(5);

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : throw new InvalidOperationException($"{variable} must be a number of seconds, not '{value}'.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        // On the API run, setup used the channel under test: close it once.
        if (_setupChannel is not null && !ReferenceEquals(_setupChannel, _channel))
            await _setupChannel.DisposeAsync();
        if (_channel is not null) await _channel.DisposeAsync();
    }
}
