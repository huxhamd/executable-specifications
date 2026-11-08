using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// What one spec carries across every domain's DSL: its aliases, and the
// outcome of its last action, whichever domain that action was in.
//
// Given steps get a context of their own from ForGiven: the same aliases, so
// "Buy milk" means the same todo in both roles, but a separate last result,
// so a Given step can never change what the spec goes on to check.
internal class SpecContext
{
    public SpecContext() : this(new Alias(), isGiven: false) { }

    private SpecContext(Alias alias, bool isGiven)
    {
        Alias = alias;
        IsGiven = isGiven;
    }

    public Alias Alias { get; }
    public bool IsGiven { get; }
    public DriverResult? LastResult { get; set; }

    // The only way to make a Given context.
    public SpecContext ForGiven() => IsGiven ? this : new SpecContext(Alias, isGiven: true);
}
