using acceptance.Core.Drivers;
using Xunit;

namespace acceptance.Core.Dsl;

// The base for each business domain's DSL: Todos now, Lists or Accounts later.
// Every domain shares the spec's context, so a spec can act in one domain and
// check the outcome from any of them.
//
// Specs live in another project and see only the public members: the DSL's
// vocabulary. Everything a DSL is built from is internal to this project, so
// the constructor and the helpers below are private protected.
public abstract class DslBase
{
    private readonly SpecContext _context;

    private protected DslBase(SpecContext context) => _context = context;

    private protected Alias Alias => _context.Alias;

    private protected void Record(DriverResult result) => _context.LastResult = result;

    // Drivers report what happened; the DSL decides whether that passes.
    private protected void ConfirmLastActionSucceeded(string because)
    {
        Assert.NotNull(_context.LastResult);
        Assert.False(_context.LastResult.IsFailed, $"{because}. The system said: {_context.LastResult.Error}");
    }

    private protected static T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(
            $"This spec uses {description}, which this channel does not support. " +
            "Limit it to the channels that do, e.g. [Fact, Channels(ChannelType.Ui)].");

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_context.LastResult);
        Assert.True(_context.LastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, _context.LastResult.Error);
        return Task.CompletedTask;
    }
}
