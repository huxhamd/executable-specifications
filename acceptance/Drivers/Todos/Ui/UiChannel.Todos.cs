using acceptance.Drivers.Todos;

// Part of UiChannel (see Drivers/Channels/UiChannel.cs), so it shares that namespace.
namespace acceptance.Drivers.Channels;

public partial class UiChannel
{
    public ITodoDriver Todos { get; } = new UiTodoDriver(page);
    public ITodoTextInputDriver? TextInput { get; } = new UiTodoTextInputDriver(page);
}
