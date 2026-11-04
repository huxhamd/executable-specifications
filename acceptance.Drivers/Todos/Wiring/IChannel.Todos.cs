using acceptance.Todos.Drivers;

// Part of IChannel (see Core/Wiring/IChannel.cs), so it shares that namespace.
namespace acceptance.Core.Drivers;

internal partial interface IChannel
{
    ITodoDriver Todos { get; }
    ITodoEntryDriver? TodoEntry => null;
}
