using acceptance.Core.Dsl;

namespace acceptance.Todos.Dsl;

// The todo domain's DSL on the channel under test: what a spec can say about
// todos for the behaviour under test. Specs reach it as Sut.Todos, so methods
// don't repeat the word: Sut.Todos.Add("Buy milk"). Its counterpart pinned
// to the API, for arranging, is ITodosApiDsl. TodosDsl implements both.
public interface ITodosSutDsl : ISutDsl
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
