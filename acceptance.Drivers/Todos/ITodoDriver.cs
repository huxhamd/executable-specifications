using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

internal interface ITodoDriver
{
    Task<DriverResult> Add(string title);
    Task<DriverResult> Add(string title, Priority priority);
    Task<DriverResult> Complete(string title);
    Task<DriverResult> Delete(string title);

    // Channels that update asynchronously wait for a yes before answering no.
    Task<bool> IsShown(string title);
    Task<bool> IsComplete(string title);
    Task<bool> HasPriority(string title, Priority priority);
    Task<bool> IsGone(string title);
}
