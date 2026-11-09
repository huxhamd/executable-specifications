namespace acceptance.Todos.Dsl;

// The todo domain's DSL pinned to the API: what a spec can arrange about
// todos before it acts, whichever channel the run is testing. Specs reach it
// as Api.Todos: Api.Todos.Add("Buy milk"). A pinned channel only arranges,
// so it lists only actions that must succeed: no Attempt..., no Confirm....
// Its counterpart on the channel under test is ITodosSutDsl. TodosDsl
// implements both.
public interface ITodosApiDsl
{
    Task Add(string title);
    Task Complete(string title);

    // Api only
    Task AddCompleted(string title);
}
