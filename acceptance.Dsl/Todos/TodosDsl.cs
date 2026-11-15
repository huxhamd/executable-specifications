using acceptance.Core.Dsl;
using acceptance.Core.Drivers;
using acceptance.Todos.Drivers;

namespace acceptance.Todos.Dsl;

internal class TodosDsl : DslBase, ITodosSutDsl, ITodosApiDsl
{
    private readonly IChannel _channel;

    private TodosDsl(IChannel channel, SpecContext context) : base(context) => _channel = channel;

    public static ITodosSutDsl ForSut(IChannel channel, SpecContext context) =>
        new TodosDsl(channel, context);

    public static ITodosApiDsl ForApi(IChannel channel, SpecContext context) =>
        new TodosDsl(channel, context.ForPinnedChannel(nameof(Api)));

    private static readonly Vocabulary<Priority> Priorities = new("priority")
    {
        ["low"] = Priority.Low,
        ["normal"] = Priority.Normal,
        ["high"] = Priority.High,
    };

    private ITodoDriver Todos => _channel.Todos;
    private ITodoEntryDriver Entry =>
        Require(_channel.TodoEntry, "live feedback while entering a todo");

    public async Task AttemptToAdd(string title, string priority) =>
        Record(await Todos.Add(Alias.For(title), Priorities.Translate(priority)));

    public async Task AttemptToAddWithTitleOfLength(int length) =>
        Record(await Todos.Add(Alias.OfLength(length), Priority.Normal));

    public async Task Add(string title, string priority)
    {
        await AttemptToAdd(title, priority);
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

    public async Task ConfirmPriority(string title, string priority) =>
        Assert.True(await Todos.HasPriority(Alias.For(title), Priorities.Translate(priority)),
            $"Expected todo '{title}' to have priority '{priority}'");

    public Task TypeTitleOfLength(int length) =>
        Entry.TypeTitle(Alias.OfLength(length));

    public async Task ConfirmWarnedTitleIsTooLong() =>
        Assert.True(await Entry.IsTitleTooLongWarningShown(),
            "Expected a warning that the title is too long");

    public async Task AddCompleted(string title, string priority)
    {
        await Add(title, priority);
        await Complete(title);
    }
}
