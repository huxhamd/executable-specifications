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

    public static Results<Created<TodoItem>, ContentHttpResult> Create(
        CreateTodoRequest request, TodoStore store)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Rejected("A title is required");

        var todo = store.Add(request.Title.Trim());
        return TypedResults.Created($"/api/todos/{todo.Id}", todo);
    }

    // Plain text, so clients get the message as-is rather than as a JSON string.
    private static ContentHttpResult Rejected(string message) =>
        TypedResults.Content(message, "text/plain", statusCode: StatusCodes.Status400BadRequest);
}
