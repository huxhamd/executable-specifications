using acceptance.Core.Dsl;

namespace acceptance.Todos.Dsl;

// The todo domain's test DSL: what a spec can say about todos for the
// behaviour under test, through the channel under test. Specs reach it as
// Todos, so methods don't repeat the word: Todos.Add("Buy milk").
// Its counterpart for arranging is ITodosGivenDsl. TodosDsl implements both.
public interface ITodosTestDsl : ITestDsl
{
    Task AttemptToAdd(string title);
    Task AttemptToAddWithTitleOfLength(int length);
    Task Add(string title);

    Task AttemptToComplete(string title);
    Task Complete(string title);

    Task ConfirmShown(string title);
    Task ConfirmComplete(string title);

    // Live feedback while entering a todo
    Task TypeTitleOfLength(int length);
    Task ConfirmWarnedTitleIsTooLong();
}
