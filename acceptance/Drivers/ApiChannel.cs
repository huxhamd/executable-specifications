namespace acceptance.Drivers;

public class ApiChannel(HttpClient http) : IChannel
{
    public ITodoDriver Todos { get; } = new ApiTodoDriver(http);
}
