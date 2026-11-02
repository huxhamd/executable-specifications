using acceptance.Todos.Drivers;

// Part of ApiChannel (see Core/Channels/ApiChannel.cs), so it shares that namespace.
namespace acceptance.Core.Channels;

public partial class ApiChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
