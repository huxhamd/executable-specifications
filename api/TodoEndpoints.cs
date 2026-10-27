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

    public const int MaxTitleLength = 255;

    public static Results<Created<TodoItem>, ValidationProblem> Create(
        CreateTodoRequest request, TodoStore store)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title))
            return InvalidTitle("A title is required");
        if (title.Length > MaxTitleLength)
            return InvalidTitle("The title is too long");

        var todo = store.Add(title);
        return TypedResults.Created($"/api/todos/{todo.Id}", todo);
    }

    private static ValidationProblem InvalidTitle(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["title"] = [message] });
}
