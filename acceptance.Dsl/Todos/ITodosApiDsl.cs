namespace acceptance.Todos.Dsl;

public interface ITodosApiDsl
{
    Task Add(string title);
    Task Complete(string title);

    Task AddCompleted(string title);
}
