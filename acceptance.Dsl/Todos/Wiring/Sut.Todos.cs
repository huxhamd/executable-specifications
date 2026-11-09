using acceptance.Todos.Dsl;

// Part of Sut (see Core/Wiring/Sut.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public partial class Sut
{
    public ITodosSutDsl Todos => field ??= TodosDsl.ForSut(_channel, _context);
}
