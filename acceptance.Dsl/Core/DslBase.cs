using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for each business domain's DSL: Todos now, Lists or Accounts later.
// Every domain shares the spec's context, so a spec can act in one domain and
// check the outcome from any of them.
//
// One DSL class serves both roles. Specs never see the class, only the
// interfaces it implements: I<Domain>TestDsl as Todos., I<Domain>GivenDsl as
// Given.Todos. Each domain's DSL builds itself for a role through its own
// ForTest and ForGiven factories, and the role comes with the context: a
// Given context records into its own last result, and turns a failed
// must-succeed action into a GivenFailedException instead of an assertion.
internal abstract class DslBase : ITestDsl
{
    private readonly SpecContext _context;

    protected DslBase(SpecContext context) => _context = context;

    protected Alias Alias => _context.Alias;

    protected void Record(DriverResult result) => _context.LastResult = result;

    // Drivers report what happened; the DSL decides whether that passes.
    protected void ConfirmLastActionSucceeded(string because)
    {
        var result = _context.LastResult
            ?? throw new InvalidOperationException("No action has been recorded to confirm.");
        if (!result.IsFailed) return;

        var failure = $"{because}. The system said: {result.Error}";
        if (_context.IsGiven) throw new GivenFailedException($"A Given step failed: {failure}");
        Assert.Fail(failure);
    }

    protected T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(_context.IsGiven
            ? $"This spec arranges {description} with Given, which goes through the API, " +
              "and the API does not support it. Add it to the API's drivers, or arrange it another way."
            : $"This spec uses {description}, which this channel does not support. " +
              "Limit it to the channels that do, e.g. [Fact, Channels(ChannelType.Ui)].");

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_context.LastResult);
        Assert.True(_context.LastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, _context.LastResult.Error);
        return Task.CompletedTask;
    }
}
