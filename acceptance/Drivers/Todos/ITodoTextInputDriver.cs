namespace acceptance.Drivers.Todos;

// Live feedback while a todo's title is being entered, before it is submitted.
public interface ITodoTextInputDriver
{
    Task TypeTodoTitle(string title);
    Task ConfirmTitleTooLongWarningIsShown();
}
