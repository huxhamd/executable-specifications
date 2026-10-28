using Microsoft.Playwright;

namespace acceptance.Drivers.Todos;

// Every UI capability drives the same page, so they share one browser session.
public partial class UiChannel(IPage page) : IChannel
{
    public ITodoDriver Todos { get; } = new UiTodoDriver(page);
    public ITodoTextInputDriver? TextInput { get; } = new UiTodoTextInputDriver(page);
}
