using System.Net.Http.Json;

namespace acceptance.Drivers;

// API contract:
//   GET    /api/todos                  -> 200 [{ id, title, completed }]
//   POST   /api/todos                  -> 201, or 400 with a message when rejected
//   POST   /api/todos/{id}/complete    -> 2xx

public class ApiTodoDriver(HttpClient http) : ITodoDriver
{
    public async Task<DriverResult> AddTodo(string title) =>
        await ToResult(await http.PostAsJsonAsync("/api/todos", new { title }));

    public async Task<DriverResult> CompleteTodo(string title)
    {
        var todo = await Find(title);
        if (todo is null)
            return DriverResult.Failed($"No todo titled '{title}'");

        return await ToResult(await http.PostAsync($"/api/todos/{todo.Id}/complete", null));
    }

    public async Task ConfirmTodoIsShown(string title) =>
        Assert.True(await Find(title) is not null, $"Expected '{title}' in GET /api/todos");

    public async Task ConfirmTodoIsComplete(string title) =>
        Assert.True((await Find(title))?.Completed == true, $"Expected '{title}' to be completed");

    private static async Task<DriverResult> ToResult(HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? DriverResult.Ok
            : DriverResult.Failed(await response.Content.ReadAsStringAsync());

    private async Task<TodoDto?> Find(string title) =>
        (await http.GetFromJsonAsync<TodoDto[]>("/api/todos"))!
            .FirstOrDefault(todo => todo.Title == title);

    private record TodoDto(int Id, string Title, bool Completed);
}