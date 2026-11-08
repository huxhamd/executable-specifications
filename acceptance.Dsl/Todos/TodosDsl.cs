using acceptance.Core.Dsl;
using acceptance.Core.Drivers;
using acceptance.Todos.Drivers;

namespace acceptance.Todos.Dsl;

// The todo domain's DSL: one implementation of everything a spec can say
// about todos, in either role. Specs never see this class, only the
// interfaces: ITodosTestDsl as Todos., ITodosGivenDsl as Given.Todos.
// A method reaches a spec only if one of them lists it.
internal class TodosDsl : DslBase, ITodosTestDsl, ITodosGivenDsl
{
    private readonly IChannel _channel;

    private TodosDsl(IChannel channel, SpecContext context) : base(context) => _channel = channel;

    // Built for one role or the other, never both: each factory hands out
    // only that role's interface, and only ForGiven gives a Given context.
    public static ITodosTestDsl ForTest(IChannel channel, SpecContext context) =>
        new TodosDsl(channel, context);

    public static ITodosGivenDsl ForGiven(IChannel channel, SpecContext context) =>
        new TodosDsl(channel, context.ForGiven());

    // Drivers are named for the domain capability they offer, never the mechanism.
    private ITodoDriver Todos => _channel.Todos;
    private ITodoEntryDriver Entry =>
        Require(_channel.TodoEntry, "live feedback while entering a todo");

    public async Task AttemptToAdd(string title) =>
        Record(await Todos.Add(Alias.For(title)));

    public async Task AttemptToAddWithTitleOfLength(int length) =>
        Record(await Todos.Add(Alias.OfLength(length)));

    public async Task Add(string title)
    {
        await AttemptToAdd(title);
        ConfirmLastActionSucceeded($"Could not add todo '{title}'");
    }

    public async Task AttemptToComplete(string title) =>
        Record(await Todos.Complete(Alias.For(title)));

    public async Task Complete(string title)
    {
        await AttemptToComplete(title);
        ConfirmLastActionSucceeded($"Could not complete todo '{title}'");
    }

    public async Task ConfirmShown(string title) =>
        Assert.True(await Todos.IsShown(Alias.For(title)),
            $"Expected todo '{title}' to be shown");

    public async Task ConfirmComplete(string title) =>
        Assert.True(await Todos.IsComplete(Alias.For(title)),
            $"Expected todo '{title}' to be complete");

    // Live feedback while entering a todo

    public Task TypeTitleOfLength(int length) =>
        Entry.TypeTitle(Alias.OfLength(length));

    public async Task ConfirmWarnedTitleIsTooLong() =>
        Assert.True(await Entry.IsTitleTooLongWarningShown(),
            "Expected a warning that the title is too long");

    // Given only: listed in ITodosGivenDsl, not ITodosTestDsl. A completed
    // todo as a starting point, in one step; completing a todo is the
    // behaviour under test in its own specs.

    public async Task AddCompleted(string title)
    {
        await Add(title);
        await Complete(title);
    }
}
