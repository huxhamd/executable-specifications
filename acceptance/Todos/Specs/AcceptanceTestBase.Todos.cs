using acceptance.Todos.Dsl;

// Part of AcceptanceTestBase (see Core/Specs/AcceptanceTestBase.cs), so it shares that namespace.
namespace acceptance.Core.Specs;

public abstract partial class AcceptanceTestBase
{
    protected TodosDsl Todos => field ??= new TodosDsl(Channel, Context);
}
