using acceptance.Core.Spec.Dsl;
using acceptance.Core.Spec.Channels;
using acceptance.Todos.Drivers;

namespace acceptance.Todos.Dsl;

// The todo domain's vocabulary: everything a spec can say about todos.
// Specs reach it as Todos, so methods don't repeat the word: Todos.Add("Buy milk").
public class TodosDsl(IChannel channel, SpecContext context) : DslBase(context)
{
    // Drivers are named for the domain capability they offer, never the mechanism.
    private ITodoDriver Todos => channel.Todos;
    private ITodoEntryDriver Entry =>
        Require(channel.TodoEntry, "live feedback while entering a todo");

    public Task AttemptToAdd(string title) =>
        AttemptToAddAliased(Alias.For(title));

    public Task AttemptToAddWithTitleOfLength(int length) =>
        AttemptToAddAliased(AliasedTitleOfLength(length));

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
        Entry.TypeTitle(AliasedTitleOfLength(length));

    public async Task ConfirmWarnedTitleIsTooLong() =>
        Assert.True(await Entry.IsTitleTooLongWarningShown(),
            "Expected a warning that the title is too long");

    // Helpers

    private async Task AttemptToAddAliased(string aliasedTitle) =>
        Record(await Todos.Add(aliasedTitle));

    // The alias suffix counts towards the length, so the system sees exactly `length` characters.
    private string AliasedTitleOfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, Alias.SuffixLength);
        return Alias.For(new string('a', length - Alias.SuffixLength));
    }
}
