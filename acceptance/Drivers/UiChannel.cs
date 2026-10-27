using Microsoft.Playwright;

namespace acceptance.Drivers;

// Every UI capability drives the same page, so they share one browser session.
public class UiChannel(IPage page) : IChannel
{
    public ITodoDriver Todos { get; } = new UiTodoDriver(page);
    public ITodoTextInputDriver? TextInput { get; } = new UiTodoTextInputDriver(page);
}
