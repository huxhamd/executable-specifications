using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// What specs reach the DSLs pinned to the API through: Api.Todos.Add("Buy milk").
// Built by AcceptanceTestBase on its pinned API channel, whichever channel
// the run is testing, and sharing the spec's context, so aliases resolve the
// same here as through Sut. A pinned channel only arranges:
// the channel under test acts and checks.
//
// Each business domain adds its pinned DSL as one part
// (<Domain>/Wiring/Api.<Domain>.cs). Every part must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
public partial class Api
{
    // What each domain's pinned DSL is built from. Private, so specs can only
    // arrange through the vocabulary; the domain parts can still use them,
    // because parts of a partial class share members.
    private readonly IChannel _channel;
    private readonly SpecContext _context;

    internal Api(IChannel channel, SpecContext context)
    {
        _channel = channel;
        _context = context;
    }
}
