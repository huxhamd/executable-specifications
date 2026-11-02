namespace acceptance.Todos.Drivers;

// Live feedback while a new todo is being entered, before it is submitted.
// An optional capability: only channels with a live entry form offer it.
public interface ITodoEntryDriver
{
    Task TypeTitle(string title);
    Task<bool> IsTitleTooLongWarningShown();
}
