using acceptance.Todos.Dsl;

// Part of AcceptanceTestBase (see Core/Harness/AcceptanceTestBase.cs), so it shares that namespace.
namespace acceptance.Core.Harness;

public abstract partial class AcceptanceTestBase
{
    protected TodosDsl Todos => field ??= new TodosDsl(Channel, Context);
}
