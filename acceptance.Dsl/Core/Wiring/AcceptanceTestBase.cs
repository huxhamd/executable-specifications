using System.Globalization;
using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

public abstract class AcceptanceTestBase : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("ACCEPTANCE_BASE_URL") ?? "http://localhost:4200";

    private IChannel? _channel;
    private IChannel Channel => _channel ?? throw new InvalidOperationException("The channel isn't open yet.");
    private SpecContext Context { get; } = new();

    protected Sut Sut => field ??= new Sut(Channel, Context);

    private IChannel? _pinnedApiChannel;
    private IChannel PinnedApiChannel =>
        _pinnedApiChannel ??= Channel as ApiChannel ?? ApiChannel.Open(BaseUrl);

    protected Api Api => field ??= new Api(PinnedApiChannel, Context);

    public async ValueTask InitializeAsync()
    {
        _channel = AcceptanceChannel.Current switch
        {
            ChannelType.Api => ApiChannel.Open(BaseUrl),
            ChannelType.Ui => await UiChannel.OpenAsync(BaseUrl, UiTimeout),
            var other => throw new InvalidOperationException($"No channel set up for {other}.")
        };
    }

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
        if (_pinnedApiChannel is not null && !ReferenceEquals(_pinnedApiChannel, _channel))
            await _pinnedApiChannel.DisposeAsync();
        if (_channel is not null) await _channel.DisposeAsync();
    }
}
