namespace acceptance.Todos.Dsl;

// The todo domain's Given DSL: what a spec can arrange about todos before
// it acts, through the API. Specs reach it as Given.Todos:
// Given.Todos.Add("Buy milk"). Only actions that must succeed: no
// Attempt..., no Confirm.... Its counterpart for the behaviour under test is
// ITodosTestDsl. TodosDsl implements both.
public interface ITodosGivenDsl
{
    Task Add(string title);
    Task Complete(string title);

    // Given only
    Task AddCompleted(string title);
}
