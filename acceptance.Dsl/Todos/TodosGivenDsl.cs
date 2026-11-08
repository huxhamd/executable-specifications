using acceptance.Core.Dsl;
using acceptance.Core.Drivers;
using acceptance.Todos.Drivers;

namespace acceptance.Todos.Dsl;

// The todo domain's Given DSL, the counterpart of TodosDsl: what a spec can
// arrange about todos before it acts, through the API.
// Given.Todos.Add("Buy milk"). Only actions that must succeed. Checking
// belongs to Todos, on the channel under test.
// Public so specs can use it; built only by Given, in this project.
public class TodosGivenDsl : GivenDslBase
{
    private readonly IChannel _channel;

    internal TodosGivenDsl(IChannel channel, SpecContext context) : base(context) => _channel = channel;

    private ITodoDriver Todos => _channel.Todos;

    public async Task Add(string title) =>
        Arranged(await Todos.Add(Alias.For(title)), $"add todo '{title}'");

    public async Task Complete(string title) =>
        Arranged(await Todos.Complete(Alias.For(title)), $"complete todo '{title}'");
}
