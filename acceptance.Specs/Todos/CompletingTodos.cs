using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

public class CompletingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanCompleteATodo()
    {
        await Given.Todos.Add("Buy milk");

        await Todos.Complete("Buy milk");
        await Todos.ConfirmComplete("Buy milk");
    }
}
