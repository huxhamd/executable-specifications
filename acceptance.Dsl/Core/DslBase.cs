using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

// The base for each business domain's DSL: Todos now, Lists or Accounts later.
// Every domain shares the spec's context, so a spec can act in one domain and
// check the outcome from any of them.
//
// One DSL class serves every channel. Specs never see the class, only the
// interfaces it implements: I<Domain>SutDsl as Sut.Todos, on the channel
// under test, and I<Domain>ApiDsl as Api.Todos, pinned to the API. Each
// domain's DSL builds itself for one of them through its own ForSut and
// ForApi factories. A pinned channel only arranges, and that role comes
// with the context: a context for arrangement records into its own last
// result, and turns a failed must-succeed action into an
// ArrangementFailedException instead of an assertion.
internal abstract class DslBase : ISutDsl
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
        if (_context.IsArrangement) throw new ArrangementFailedException($"An arrangement failed: {failure}");
        Assert.Fail(failure);
    }

    protected T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(_context.IsArrangement
            ? $"This spec arranges {description} through Api., " +
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
