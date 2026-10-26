namespace acceptance.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddTodo()
    {
        await User.AddTodo("Buy milk");
        await User.ConfirmTodoIsShown("Buy milk");
    }
}