using acceptance.Todos.Drivers;

// Part of UiChannel (see Core/Channels/UiChannel.cs), so it shares that namespace.
namespace acceptance.Core.Channels;

// One todo driver for the browser, handed out once per capability it offers.
internal partial class UiChannel
{
    private readonly UiTodoDriver _todos = new(page);

    public ITodoDriver Todos => _todos;
    public ITodoEntryDriver? TodoEntry => _todos;
}
