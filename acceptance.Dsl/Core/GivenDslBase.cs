using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for each business domain's Given DSL, the counterpart of DslBase:
// Given.Todos.Add("Buy milk"). A Given DSL arranges what a spec needs; it
// never checks anything. So it offers only actions that must succeed: no
// Attempt..., no Confirm..., and nothing it does is recorded as the spec's
// last result, so a Given step can't change what a later ConfirmErrorIsShown
// is checking.
//
// Specs live in another project and see only the public members. What a
// Given DSL is built from is internal, so the constructor and the helpers
// below are private protected.
public abstract class GivenDslBase
{
    private readonly SpecContext _context;

    private protected GivenDslBase(SpecContext context) => _context = context;

    private protected Alias Alias => _context.Alias;

    // Stops the spec if a Given step didn't work. A GivenFailedException,
    // not an assertion, so the report tells a broken precondition apart from
    // the behaviour under test failing.
    private protected static void Arranged(DriverResult result, string action)
    {
        if (result.IsFailed)
            throw new GivenFailedException($"A Given step failed: could not {action}. The system said: {result.Error}");
    }

    // Given always goes through the API, so a capability the API lacks can't
    // be used to arrange anything.
    private protected static T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(
            $"This spec arranges {description} with Given, which goes through the API, " +
            "and the API does not support it. Add it to the API's drivers, or arrange it another way.");
}
