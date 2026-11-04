using acceptance.Todos.Drivers;

// Part of ApiChannel (see Core/Wiring/ApiChannel.cs), so it shares that namespace.
namespace acceptance.Core.Drivers;

internal partial class ApiChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
