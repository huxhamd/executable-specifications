namespace acceptance.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddATodo()
    {
        await User.AddTodo("Buy milk");
        await User.ConfirmTodoIsShown("Buy milk");
    }
    
    [Fact]
    public async Task UserCannotAddATodoWithoutATitle()
    {
        await User.AttemptToAddTodo("");
        await User.ConfirmErrorIsShown("A title is required");
    }

    [Fact]
    public async Task UserCannotAddATodoWithATitleLongerThan255Utf16Characters()
    {
        await User.AttemptToAddTodoWithTitleOfLength(256);
        await User.ConfirmErrorIsShown("The title is too long");
    }
}