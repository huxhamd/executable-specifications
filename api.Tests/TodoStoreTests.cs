namespace Todo.Api.Tests;

public class TodoStoreTests
{
    private readonly TodoStore _store = new();

    [Fact]
    public void StartsEmpty() =>
        Assert.Empty(_store.All());

    [Fact]
    public void AddReturnsAnIncompleteTodoWithTheGivenTitle()
    {
        var todo = _store.Add("Buy milk");

        Assert.Equal("Buy milk", todo.Title);
        Assert.False(todo.Completed);
    }

    [Fact]
    public void AddAssignsAUniqueIdToEachTodo()
    {
        var first = _store.Add("Buy milk");
        var second = _store.Add("Buy milk");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void AllReturnsAddedTodosInTheOrderTheyWereAdded()
    {
        var first = _store.Add("Buy milk");
        var second = _store.Add("Walk dog");

        Assert.Equal([first, second], _store.All());
    }

    [Fact]
    public async Task ConcurrentAddsAreAllKeptWithDistinctIds()
    {
        await Task.WhenAll(Enumerable.Range(0, 1000)
            .Select(i => Task.Run(() => _store.Add($"Todo {i}"))));

        var todos = _store.All();
        Assert.Equal(1000, todos.Count);
        Assert.Equal(1000, todos.Select(todo => todo.Id).Distinct().Count());
    }
}
