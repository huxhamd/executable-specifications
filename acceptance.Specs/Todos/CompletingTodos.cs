using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

public class CompletingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanCompleteATodo()
    {
        await Api.Todos.Add("Buy milk");

        await Sut.Todos.Complete("Buy milk");
        await Sut.Todos.ConfirmComplete("Buy milk");
    }
}
