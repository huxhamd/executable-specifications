namespace acceptance.Todos.Drivers;

internal interface ITodoEntryDriver
{
    Task TypeTitle(string title);
    Task<bool> IsTitleTooLongWarningShown();
}
