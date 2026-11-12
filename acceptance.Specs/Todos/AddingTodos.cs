using acceptance.Core.Dsl;

namespace acceptance.Todos.Specs;

public class AddingTodos : AcceptanceTestBase
{
    [Fact]
    public async Task UserCanAddATodo()
    {
        await Sut.Todos.Add(title: "Buy milk");
        await Sut.Todos.ConfirmShown(title: "Buy milk");
    }
    
    [Fact]
    public async Task UserCannotAddATodoWithoutATitle()
    {
        await Sut.Todos.AttemptToAdd(title: "");
        await Sut.Todos.ConfirmErrorIsShown(message: "A title is required");
    }

    [Fact]
    public async Task UserCannotAddATodoWithATitleLongerThan255Utf16Characters()
    {
        await Sut.Todos.AttemptToAddWithTitleOfLength(length: 256);
        await Sut.Todos.ConfirmErrorIsShown(message: "The title is too long");
    }

    [Fact, Channels(ChannelType.Ui)]
    public async Task UserIsWarnedWhileTypingATitleLongerThan255Utf16Characters()
    {
        await Sut.Todos.TypeTitleOfLength(length: 256);
        await Sut.Todos.ConfirmWarnedTitleIsTooLong();
    }
}