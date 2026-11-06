using acceptance.Todos.Dsl;

// Part of GivenDsl (see Core/Wiring/GivenDsl.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public partial class GivenDsl
{
    public GivenTodosDsl Todos => field ??= new GivenTodosDsl(_channel, _context);
}
