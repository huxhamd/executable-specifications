using acceptance.Core.Dsl;

namespace acceptance.Todos.Dsl;

public interface ITodosSutDsl : ISutDsl
{
    Task AttemptToAdd(string title);
    Task AttemptToAddWithTitleOfLength(int length);
    Task Add(string title);

    Task AttemptToComplete(string title);
    Task Complete(string title);

    Task ConfirmShown(string title);
    Task ConfirmComplete(string title);

    Task TypeTitleOfLength(int length);
    Task ConfirmWarnedTitleIsTooLong();
}
