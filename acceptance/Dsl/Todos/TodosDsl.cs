using acceptance.Drivers.Channels;
using acceptance.Drivers.Todos;

namespace acceptance.Dsl.Todos;

// The todo domain's vocabulary: everything a spec can say about todos.
// Specs reach it as Todos, so methods don't repeat the word: Todos.Add("Buy milk").
public class TodosDsl(IChannel channel, SpecContext context) : DslBase(context)
{
    private ITodoDriver Driver => channel.Todos;

    private ITodoTextInputDriver TextInput => channel.TextInput
        ?? throw new InvalidOperationException(
            "This spec uses live text input, which this channel does not support. " +
            "Limit it to the channels that do, e.g. [Fact, Channels(Channel.Ui)].");

    public Task AttemptToAdd(string title) =>
        AttemptToAddAliased(Alias.For(title));

    public Task AttemptToAddWithTitleOfLength(int length) =>
        AttemptToAddAliased(AliasedTitleOfLength(length));

    public async Task Add(string title)
    {
        await AttemptToAdd(title);
        LastResult!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToComplete(string title) =>
        Record(await Driver.CompleteTodo(Alias.For(title)));

    public async Task Complete(string title)
    {
        await AttemptToComplete(title);
        LastResult!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmShown(string title) =>
        Driver.ConfirmTodoIsShown(Alias.For(title));

    public Task ConfirmComplete(string title) =>
        Driver.ConfirmTodoIsComplete(Alias.For(title));

    // Live text input

    public Task TypeTitleOfLength(int length) =>
        TextInput.TypeTodoTitle(AliasedTitleOfLength(length));

    public Task ConfirmWarnedTitleIsTooLong() =>
        TextInput.ConfirmTitleTooLongWarningIsShown();

    // Helpers

    private async Task AttemptToAddAliased(string aliasedTitle) =>
        Record(await Driver.AddTodo(aliasedTitle));

    // The alias suffix counts towards the length, so the system sees exactly `length` characters.
    private string AliasedTitleOfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, Alias.SuffixLength);
        return Alias.For(new string('a', length - Alias.SuffixLength));
    }
}
