using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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

        var created = Assert.IsType<Created<TodoItem>>(result.Result);
        var stored = Assert.Single(_store.All());
        Assert.Equal(stored, created.Value);
        Assert.Equal($"/api/todos/{stored.Id}", created.Location);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public void CreateRejectsAMissingOrBlankTitle(string? title)
    {
        var result = TodoEndpoints.Create(new CreateTodoRequest(title!), _store);

        var rejection = Assert.IsType<ContentHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, rejection.StatusCode);
        Assert.Equal("text/plain", rejection.ContentType);
        Assert.Equal("A title is required", rejection.ResponseContent);
        Assert.Empty(_store.All());
    }

    [Fact]
    public void CreateTrimsWhitespaceAroundTheTitle()
    {
        var result = TodoEndpoints.Create(new CreateTodoRequest(" \tBuy  milk\n "), _store);

        Assert.IsType<Created<TodoItem>>(result.Result);
        Assert.Equal("Buy  milk", Assert.Single(_store.All()).Title);
    }
}
