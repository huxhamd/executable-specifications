using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// Everything a spec can arrange before it acts, reached as Given:
// Given.Todos.Add("Buy milk"). Built by AcceptanceTestBase on the setup
// channel (always the API), and sharing the spec's context, so aliases
// resolve the same here as in the DSLs that act and check.
//
// Each business domain adds its setup vocabulary as one part
// (<Domain>/Wiring/GivenDsl.<Domain>.cs). Every part must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
public partial class GivenDsl
{
    // What each domain's setup DSL is built from. Private, so specs can only
    // arrange through the vocabulary; the domain parts can still use them,
    // because parts of a partial class share members.
    private readonly IChannel _channel;
    private readonly SpecContext _context;

    internal GivenDsl(IChannel channel, SpecContext context)
    {
        _channel = channel;
        _context = context;
    }
}
