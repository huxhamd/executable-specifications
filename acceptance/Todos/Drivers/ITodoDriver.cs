using acceptance.Core.Spec;

namespace acceptance.Todos.Drivers;

// What every channel can do with todos. The domain is in the type name,
// so methods don't repeat it: ITodoDriver.Add, not AddTodo.
public interface ITodoDriver
{
    Task<DriverResult> Add(string title);
    Task<DriverResult> Complete(string title);
    Task ConfirmShown(string title);
    Task ConfirmComplete(string title);
}
