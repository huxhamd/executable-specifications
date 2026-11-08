using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// What specs reach the Given DSLs through, the counterpart of
// AcceptanceTestBase: Given.Todos.Add("Buy milk"). Built by
// AcceptanceTestBase on the Given channel (always the API), and sharing the
// spec's context, so aliases resolve the same here as in the DSLs that act
// and check.
//
// Each business domain adds its Given DSL as one part
// (<Domain>/Wiring/Given.<Domain>.cs). Every part must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
public partial class Given
{
    // What each domain's Given DSL is built from. Private, so specs can only
    // arrange through the vocabulary; the domain parts can still use them,
    // because parts of a partial class share members.
    private readonly IChannel _channel;
    private readonly SpecContext _context;

    internal Given(IChannel channel, SpecContext context)
    {
        _channel = channel;
        _context = context;
    }
}
