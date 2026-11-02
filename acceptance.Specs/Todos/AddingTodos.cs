using acceptance.Core.Specs;

namespace acceptance.Todos.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddATodo()
    {
        await Todos.Add("Buy milk");
        await Todos.ConfirmShown("Buy milk");
    }
    
    [Fact]
    public async Task UserCannotAddATodoWithoutATitle()
    {
        await Todos.AttemptToAdd("");
        await Todos.ConfirmErrorIsShown("A title is required");
    }

    [Fact]
    public async Task UserCannotAddATodoWithATitleLongerThan255Utf16Characters()
    {
        await Todos.AttemptToAddWithTitleOfLength(256);
        await Todos.ConfirmErrorIsShown("The title is too long");
    }

    [Fact, Channels(ChannelType.Ui)]
    public async Task UserIsWarnedWhileTypingATitleLongerThan255Utf16Characters()
    {
        await Todos.TypeTitleOfLength(256);
        await Todos.ConfirmWarnedTitleIsTooLong();
    }
}