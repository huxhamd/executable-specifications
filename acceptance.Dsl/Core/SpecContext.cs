using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

internal class SpecContext
{
    public SpecContext() : this(new Alias(), pinnedChannel: null) { }

    private SpecContext(Alias alias, string? pinnedChannel)
    {
        Alias = alias;
        PinnedChannel = pinnedChannel;
    }

    public Alias Alias { get; }
    public string? PinnedChannel { get; }
    public bool IsPinnedChannel => PinnedChannel is not null;
    public DriverResult? LastResult { get; set; }

    public SpecContext ForPinnedChannel(string channel) =>
        new(Alias, channel);
}
