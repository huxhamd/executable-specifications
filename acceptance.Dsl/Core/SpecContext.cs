using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// What one spec carries across every domain's DSL: its aliases, and the
// outcome of its last action, whichever domain that action was in.
//
// Steps on a pinned channel (Api.) only arrange, and get a context of their
// own from ForArrangement: the same aliases, so "Buy milk" means
// the same todo on every channel, but a separate last result, so arranging
// can never change what the spec goes on to check.
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

    // The only way to make a context for arrangement.
    public SpecContext ForArrangement() =>
        IsArrangement ? this : new SpecContext(Alias, isArrangement: true);
}
