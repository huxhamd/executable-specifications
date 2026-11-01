using acceptance.Core.Spec;
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
        LastResult!.ShouldSucceed($"Could not add todo '{title}'");
    }

    public async Task AttemptToComplete(string title) =>
        Record(await Todos.Complete(Alias.For(title)));

    public async Task Complete(string title)
    {
        await AttemptToComplete(title);
        LastResult!.ShouldSucceed($"Could not complete todo '{title}'");
    }

    public Task ConfirmShown(string title) =>
        Todos.ConfirmShown(Alias.For(title));

    public Task ConfirmComplete(string title) =>
        Todos.ConfirmComplete(Alias.For(title));

    // Live feedback while entering a todo

    public Task TypeTitleOfLength(int length) =>
        Entry.TypeTitle(AliasedTitleOfLength(length));

    public Task ConfirmWarnedTitleIsTooLong() =>
        Entry.ConfirmTitleTooLongWarningShown();

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
