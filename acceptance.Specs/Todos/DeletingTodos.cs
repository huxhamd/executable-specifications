using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

public class DeletingTodos : AcceptanceTestBase
{
    [Fact, Pending]
    public async Task UserCanDeleteATodo()
    {
        await Api.Todos.Add(title: "Buy milk");

        await Sut.Todos.Delete(title: "Buy milk");
        await Sut.Todos.ConfirmNotShown(title: "Buy milk");
    }

    [Fact, Pending]
    public async Task DeletingATodoLeavesTheOthers()
    {
        await Api.Todos.Add(title: "Buy milk");
        await Api.Todos.Add(title: "Pay rent");

        await Sut.Todos.Delete(title: "Buy milk");
        await Sut.Todos.ConfirmNotShown(title: "Buy milk");
        await Sut.Todos.ConfirmShown(title: "Pay rent");
    }
}
