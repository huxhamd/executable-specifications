using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for each business domain's setup vocabulary, reached through
// Given: Given.Todos.Add("Buy milk"). Setup arranges what a spec needs; it
// never checks anything. So a setup DSL offers only actions that must
// succeed: no Attempt..., no Confirm..., and nothing it does is recorded as
// the spec's last result, so a Given step can't change what a later
// ConfirmErrorIsShown is checking.
//
// Specs live in another project and see only the public members. What a
// setup DSL is built from is internal, so the constructor and the helpers
// below are private protected.
public abstract class SetupDslBase
{
    private readonly SpecContext _context;

    private protected SetupDslBase(SpecContext context) => _context = context;

    private protected Alias Alias => _context.Alias;

    // Stops the spec if setup didn't work. A SetupFailedException, not an
    // assertion, so the report tells a broken precondition apart from the
    // behaviour under test failing.
    private protected static void Arranged(DriverResult result, string action)
    {
        if (result.IsFailed)
            throw new SetupFailedException($"Setup failed: could not {action}. The system said: {result.Error}");
    }

    // Setup always goes through the API, so a capability the API lacks can't
    // be used to arrange anything.
    private protected static T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(
            $"This spec arranges {description} with Given, which goes through the API, " +
            "and the API does not support it. Add it to the API's drivers, or arrange it another way.");
}

public sealed class SetupFailedException(string message) : Exception(message);
