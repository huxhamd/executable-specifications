using acceptance.Todos.Drivers;

// Part of UiChannel (see Core/Spec/Channels/UiChannel.cs), so it shares that namespace.
namespace acceptance.Core.Spec.Channels;

public partial class UiChannel
{
    public ITodoDriver Todos { get; } = new UiTodoDriver(page);
    public ITodoTextInputDriver? TextInput { get; } = new UiTodoTextInputDriver(page);
}
