namespace Todo.Api;

public record TodoItem(int Id, string Title, bool Completed);

// In-memory for now: todos are lost when the API restarts.
public class TodoStore
{
    private readonly List<TodoItem> _todos = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<TodoItem> All()
    {
        lock (_lock) return [.. _todos];
    }

    public TodoItem Add(string title)
    {
        lock (_lock)
        {
            var todo = new TodoItem(_todos.Count + 1, title, Completed: false);
            _todos.Add(todo);
            return todo;
        }
    }
}
