namespace Todo.Api.Tests;

public class TodoEndpointsTests
{
    private readonly TodoStore _store = new();

    [Fact]
    public void ListReturnsEveryStoredTodo()
    {
        var todo = _store.Add("Buy milk");

        var result = TodoEndpoints.List(_store);

        Assert.Equal([todo], result.Value);
    }

    [Fact]
    public void CreateStoresTheTodo()
    {
        TodoEndpoints.Create(new CreateTodoRequest("Buy milk"), _store);

        Assert.Equal("Buy milk", Assert.Single(_store.All()).Title);
    }

    [Fact]
    public void CreateReturnsTheNewTodoAndItsLocation()
    {
        var result = TodoEndpoints.Create(new CreateTodoRequest("Buy milk"), _store);

        var stored = Assert.Single(_store.All());
        Assert.Equal(stored, result.Value);
        Assert.Equal($"/api/todos/{stored.Id}", result.Location);
    }
}
