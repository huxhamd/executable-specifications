using acceptance.Drivers.Todos;

// Part of ApiChannel (see Drivers/Channels/ApiChannel.cs), so it shares that namespace.
namespace acceptance.Drivers.Channels;

public partial class ApiChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
