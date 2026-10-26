using Microsoft.AspNetCore.Http.HttpResults;

namespace Todo.Api;

public record CreateTodoRequest(string Title);

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/api/todos");
        todos.MapGet("/", List);
        todos.MapPost("/", Create);
    }

    public static Ok<IReadOnlyList<TodoItem>> List(TodoStore store) =>
        TypedResults.Ok(store.All());

    public static Created<TodoItem> Create(CreateTodoRequest request, TodoStore store)
    {
        var todo = store.Add(request.Title);
        return TypedResults.Created($"/api/todos/{todo.Id}", todo);
    }
}
