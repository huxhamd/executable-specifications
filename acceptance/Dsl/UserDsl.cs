using acceptance.Drivers;
using acceptance.Drivers.Todos;

namespace acceptance.Dsl;

public class UserDsl(IChannel channel, Alias alias)
{
    private DriverResult? _lastResult;

    private ITodoDriver Todos => channel.Todos;

    private ITodoTextInputDriver TextInput => channel.TextInput
        ?? throw new InvalidOperationException(
            "This spec uses live text input, which this channel does not support. " +
            "Limit it to the channels that do, e.g. [Fact, Channels(Channel.Ui)].");

    public Task AttemptToAddTodoWithTitleOfLength(int length) =>
        AttemptToAddAliasedTodo(AliasedTitleOfLength(length));

    public Task AttemptToAddTodo(string title) =>
        AttemptToAddAliasedTodo(alias.For(title));

    public async Task AddTodo(string title)
    {
        await AttemptToAddTodo(title);
        _lastResult!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToCompleteTodo(string title) =>
        _lastResult = await Todos.CompleteTodo(alias.For(title));

    public async Task CompleteTodo(string title)
    {
        await AttemptToCompleteTodo(title);
        _lastResult!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmTodoIsShown(string title) =>
        Todos.ConfirmTodoIsShown(alias.For(title));

    public Task ConfirmTodoIsComplete(string title) =>
        Todos.ConfirmTodoIsComplete(alias.For(title));

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_lastResult);
        Assert.True(_lastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Equal(message, _lastResult.Error);
        return Task.CompletedTask;
    }

    // Live text input

    public Task TypeTodoTitleOfLength(int length) =>
        TextInput.TypeTodoTitle(AliasedTitleOfLength(length));

    public Task ConfirmWarnedTitleIsTooLong() =>
        TextInput.ConfirmTitleTooLongWarningIsShown();

    // Helpers

    private async Task AttemptToAddAliasedTodo(string aliasedTitle) =>
        _lastResult = await Todos.AddTodo(aliasedTitle);

    // The alias suffix counts towards the length, so the system sees exactly `length` characters.
    private string AliasedTitleOfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, Alias.SuffixLength);
        return alias.For(new string('a', length - Alias.SuffixLength));
    }
}
