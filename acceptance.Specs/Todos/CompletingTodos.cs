using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

public class CompletingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanCompleteATodo()
    {
        await Api.Todos.Add(title: "Buy milk");

        await Sut.Todos.Complete(title: "Buy milk");
        await Sut.Todos.ConfirmComplete(title: "Buy milk");
    }
}
