using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddATodo()
    {
        await Sut.Todos.Add("Buy milk");
        await Sut.Todos.ConfirmShown("Buy milk");
    }
    
    [Fact]
    public async Task UserCannotAddATodoWithoutATitle()
    {
        await Sut.Todos.AttemptToAdd("");
        await Sut.Todos.ConfirmErrorIsShown("A title is required");
    }

    [Fact]
    public async Task UserCannotAddATodoWithATitleLongerThan255Utf16Characters()
    {
        await Sut.Todos.AttemptToAddWithTitleOfLength(256);
        await Sut.Todos.ConfirmErrorIsShown("The title is too long");
    }

    [Fact, Channels(ChannelType.Ui)]
    public async Task UserIsWarnedWhileTypingATitleLongerThan255Utf16Characters()
    {
        await Sut.Todos.TypeTitleOfLength(256);
        await Sut.Todos.ConfirmWarnedTitleIsTooLong();
    }
}