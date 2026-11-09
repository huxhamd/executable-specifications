using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// What specs reach the DSLs on the channel under test through:
// Sut.Todos.Complete("Buy milk"). Built by AcceptanceTestBase on the channel
// this run drives, so the same spec acts and checks through the API on one
// run and the browser on another. It shares the spec's context with Api, so
// "Buy milk" means the same todo whichever channel set it up.
//
// Each business domain adds its DSL as one part
// (<Domain>/Wiring/Sut.<Domain>.cs). Every part must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
public partial class Sut
{
    // What each domain's DSL is built from. Private, so specs can only reach
    // the system through the vocabulary; the domain parts can still use them,
    // because parts of a partial class share members.
    private readonly IChannel _channel;
    private readonly SpecContext _context;

    internal Sut(IChannel channel, SpecContext context)
    {
        _channel = channel;
        _context = context;
    }
}
