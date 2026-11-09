using acceptance.Todos.Dsl;

// Part of Api (see Core/Wiring/Api.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public partial class Api
{
    public ITodosApiDsl Todos => field ??= TodosDsl.ForApi(_channel, _context);
}
