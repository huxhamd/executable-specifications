using acceptance.Todos.Dsl;

// Part of Given (see Core/Wiring/Given.cs), so it shares that namespace.
namespace acceptance.Core.Dsl;

public partial class Given
{
    public TodosGivenDsl Todos => field ??= new TodosGivenDsl(_channel, _context);
}
