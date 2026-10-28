namespace acceptance.Drivers.Todos;

public partial class ApiChannel(HttpClient http) : IChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
