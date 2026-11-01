using acceptance.Todos.Drivers;

// Part of IChannel (see Core/Spec/Channels/IChannel.cs), so it shares that namespace.
namespace acceptance.Core.Spec.Channels;

public partial interface IChannel
{
    ITodoDriver Todos { get; }
    ITodoTextInputDriver? TextInput => null;
}
