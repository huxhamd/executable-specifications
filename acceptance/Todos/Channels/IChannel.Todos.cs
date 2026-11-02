using acceptance.Todos.Drivers;

// Part of IChannel (see Core/Channels/IChannel.cs), so it shares that namespace.
namespace acceptance.Core.Channels;

internal partial interface IChannel
{
    ITodoDriver Todos { get; }
    ITodoEntryDriver? TodoEntry => null;
}
