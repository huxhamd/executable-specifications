using acceptance.Drivers.Todos;

// Part of IChannel (see Drivers/Channels/IChannel.cs), so it shares that namespace.
namespace acceptance.Drivers.Channels;

public partial interface IChannel
{
    ITodoDriver Todos { get; }

    ITodoTextInputDriver? TextInput => null;
}
