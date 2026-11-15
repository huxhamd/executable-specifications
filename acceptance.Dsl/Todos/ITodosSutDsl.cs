using acceptance.Core.Dsl;

namespace acceptance.Todos.Dsl;

public interface ITodosSutDsl : ISutDsl
{
    Task AttemptToAdd(string title);
    Task AttemptToAdd(string title, string priority);
    Task AttemptToAddWithTitleOfLength(int length);
    Task Add(string title);
    Task Add(string title, string priority);

    Task AttemptToComplete(string title);
    Task Complete(string title);

    Task ConfirmShown(string title);
    Task ConfirmComplete(string title);
    Task ConfirmPriority(string title, string priority);

    Task TypeTitleOfLength(int length);
    Task ConfirmWarnedTitleIsTooLong();
}
