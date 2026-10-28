using System.Net.Http.Json;

namespace acceptance.Drivers.Todos;

// API contract:
//   GET    /api/todos                  -> 200 [{ id, title, completed }]
//   POST   /api/todos                  -> 201, or 400 ValidationProblemDetails when rejected:
//                                         { errors: { "<field>": ["<message>", ...] } },
//                                         e.g. errors.title = ["A title is required"]
//   POST   /api/todos/{id}/complete    -> 2xx

public class ApiTodoDriver(HttpClient http) : ITodoDriver
{
    public async Task<DriverResult> AddTodo(string title) =>
        await ToResult(await http.PostAsJsonAsync("/api/todos", new { title }));

    public async Task<DriverResult> CompleteTodo(string title)
    {
        var todo = await Find(title);
        if (todo is null)
            return DriverResult.Fail($"No todo titled '{title}'");

        return await ToResult(await http.PostAsync($"/api/todos/{todo.Id}/complete", null));
    }

    public async Task ConfirmTodoIsShown(string title) =>
        Assert.True(await Find(title) is not null, $"Expected '{title}' in GET /api/todos");

    public async Task ConfirmTodoIsComplete(string title) =>
        Assert.True((await Find(title))?.Completed == true, $"Expected '{title}' to be completed");

    private static async Task<DriverResult> ToResult(HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? DriverResult.Okay
            : DriverResult.Fail(await BodyOrProblem(response));
    
    private static async Task<string> BodyOrProblem(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
            return await response.Content.ReadAsStringAsync();

        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        return problem?.Errors?.Values.SelectMany(messages => messages).FirstOrDefault()
            ?? problem?.Detail
            ?? problem?.Title
            ?? "";
    }

    private async Task<TodoDto?> Find(string title) =>
        (await http.GetFromJsonAsync<TodoDto[]>("/api/todos"))!
            .FirstOrDefault(todo => todo.Title == title);

    private record TodoDto(int Id, string Title, bool Completed);
    private record ProblemDto(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}