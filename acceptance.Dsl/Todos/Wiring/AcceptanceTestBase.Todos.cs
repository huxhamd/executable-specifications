using acceptance.Todos.Dsl;

// Part of AcceptanceTestBase (see Core/Wiring/AcceptanceTestBase.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public abstract partial class AcceptanceTestBase
{
    protected ITodosTestDsl Todos => field ??= TodosDsl.ForTest(Channel, Context);
}
