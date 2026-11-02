using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

// What every channel can do with todos. The domain is in the type name,
// so methods don't repeat it: ITodoDriver.Add, not AddTodo.
public interface ITodoDriver
{
    Task<DriverResult> Add(string title);
    Task<DriverResult> Complete(string title);

    // Observations: drivers report what they see, and the DSL decides whether
    // that passes. Channels that update asynchronously wait for a yes before
    // answering no.
    Task<bool> IsShown(string title);
    Task<bool> IsComplete(string title);
}
