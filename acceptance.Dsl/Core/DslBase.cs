using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

internal abstract class DslBase : ISutDsl
{
    private readonly SpecContext _context;

    protected DslBase(SpecContext context) => _context = context;

    protected Alias Alias => _context.Alias;

    protected void Record(DriverResult result) => _context.LastResult = result;

    protected void ConfirmLastActionSucceeded(string because)
    {
        var result = _context.LastResult
            ?? throw new InvalidOperationException("No action has been recorded to confirm.");
        if (!result.IsFailed) return;

        var failure = $"{because}. The system said: {result.Error}";
        if (_context.IsPinnedChannel)
            throw new ArrangementFailedException($"An arrangement through {_context.PinnedChannel} failed: {failure}");
        Assert.Fail(failure);
    }

    protected T Require<T>(T? capability, string description) where T : class =>
        capability ?? throw new InvalidOperationException(_context.IsPinnedChannel
            ? $"This spec uses {description} through {_context.PinnedChannel}, which does not support it. " +
              "Add it to that channel's drivers, or reach it another way."
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
