namespace acceptance.Todos.Dsl;

public interface ITodosApiDsl
{
    Task Add(string title, string priority = "normal");
    Task Complete(string title);

    Task AddCompleted(string title, string priority = "normal");
}
