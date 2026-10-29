using acceptance.Drivers;

namespace acceptance.Dsl;

// The base for each business domain's DSL: Todos now, Lists or Accounts later.
// Every domain shares the spec's context, so a spec can act in one domain and
// check the outcome from any of them.
public abstract class DslBase(SpecContext context)
{
    protected Alias Alias => context.Alias;

    protected DriverResult? LastResult => context.LastResult;

    protected void Record(DriverResult result) => context.LastResult = result;

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(context.LastResult);
        Assert.True(context.LastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, context.LastResult.Error);
        return Task.CompletedTask;
    }
}
