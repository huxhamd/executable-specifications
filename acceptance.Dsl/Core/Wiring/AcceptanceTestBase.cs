using System.Globalization;
using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for every spec. Specs reach the system through two roots, named
// for the channel each one drives: Sut, the channel under test, and Api,
// pinned to the API on every run. Each business domain adds its DSL as one
// part of each (<Domain>/Wiring/Sut.<Domain>.cs and Api.<Domain>.cs), so
// specs arrange with Api.Todos.Add("Buy milk"), then act and check with
// Sut.Todos.Complete("Buy milk"). The specs themselves live in the
// acceptance.Specs project, which sees only what's public here: this class,
// Sut, Api, the DSLs they expose, ChannelType and the Channels attribute.
public abstract class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    // What the DSLs are built from: the channel this run drives, and the
    // context every domain shares within one spec. Private, so specs can
    // only reach the system through Sut and Api.
    private IChannel? _channel;
    private IChannel Channel => _channel ?? throw new InvalidOperationException("The channel isn't open yet.");
    private SpecContext Context { get; } = new();

    protected Sut Sut => field ??= new Sut(Channel, Context);

    // What a spec arranges before it acts goes through the API on every run:
    // it's the fastest way in that the real system supports, and the journey
    // through each channel is the business of the specs that test it. On the
    // API run that's the channel already open. On other runs a second
    // channel opens the first time a spec uses Api, and specs that don't
    // use it never open one. Api shares Context with Sut, so "Buy milk"
    // means the same todo whichever channel set it up.
    private IChannel? _pinnedApiChannel;
    private IChannel PinnedApiChannel =>
        _pinnedApiChannel ??= Channel as ApiChannel ?? ApiChannel.Open(BaseUrl);

    protected Api Api => field ??= new Api(PinnedApiChannel, Context);

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
        // On the API run, Api used the channel under test: close it once.
        if (_pinnedApiChannel is not null && !ReferenceEquals(_pinnedApiChannel, _channel))
            await _pinnedApiChannel.DisposeAsync();
        if (_channel is not null) await _channel.DisposeAsync();
    }
}
