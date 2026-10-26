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
    public async Task UserCannotAddATodoWithATitleThatIsTooLong()
    {
        var longTitle = new string('a', 256); // Assuming 255 is the maximum length
        await User.AttemptToAddTodo(longTitle);
        await User.ConfirmErrorIsShown("The title is too long");
    }
}