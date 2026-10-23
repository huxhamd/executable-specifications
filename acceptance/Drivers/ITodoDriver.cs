namespace acceptance.Drivers;

public interface ITodoDriver
{
    Task<DriverResult> AddTodo(string title);
    Task<DriverResult> CompleteTodo(string title);
    Task ConfirmTodoIsShown(string title);
    Task ConfirmTodoIsComplete(string title);
}