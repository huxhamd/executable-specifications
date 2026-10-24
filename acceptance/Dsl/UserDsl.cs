using acceptance.Drivers;

namespace acceptance.Dsl;

public class UserDsl(ITodoDriver driver, Alias alias)
{
    private DriverResult? _lastResult;

    public async Task AttemptToAddTodo(string title) =>
        _lastResult = await driver.AddTodo(alias.For(title));

    public async Task AddTodo(string title)
    {
        await AttemptToAddTodo(title);
        _lastResult!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToCompleteTodo(string title) =>
        _lastResult = await driver.CompleteTodo(alias.For(title));

    public async Task CompleteTodo(string title)
    {
        await AttemptToCompleteTodo(title);
        _lastResult!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmTodoIsShown(string title) =>
        driver.ConfirmTodoIsShown(alias.For(title));

    public Task ConfirmTodoIsComplete(string title) =>
        driver.ConfirmTodoIsComplete(alias.For(title));

    public Task ConfirmErrorIsShown(string message)
    {
        Assert.NotNull(_lastResult);
        Assert.True(_lastResult.IsFailed, "Expected the last action to fail, but it succeeded.");
        Assert.Contains(message, _lastResult.Error, StringComparison.Ordinal);
        return Task.CompletedTask;
    }
}