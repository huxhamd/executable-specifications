using acceptance.Todos.Dsl;

// Part of Given (see Core/Wiring/Given.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public partial class Given
{
    public ITodosGivenDsl Todos => field ??= TodosDsl.ForGiven(_channel, _context);
}
