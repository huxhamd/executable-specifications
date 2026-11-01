using acceptance.Core.Spec.Drivers;

namespace acceptance.Core.Spec.Dsl;

// What one spec carries across every domain's DSL: its aliases, and the
// outcome of its last action, whichever domain that action was in.
public class SpecContext
{
    public Alias Alias { get; } = new();
    public DriverResult? LastResult { get; set; }
}
