using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

internal class SpecContext
{
    public SpecContext() : this(new Alias(), isArrangement: false) { }

    private SpecContext(Alias alias, bool isArrangement)
    {
        Alias = alias;
        IsArrangement = isArrangement;
    }

    public Alias Alias { get; }
    public bool IsArrangement { get; }
    public DriverResult? LastResult { get; set; }

    public SpecContext ForArrangement() =>
        IsArrangement ? this : new SpecContext(Alias, isArrangement: true);
}
