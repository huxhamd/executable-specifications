namespace acceptance.Drivers;

// The capabilities one channel offers. Each is a small driver of its own.
// Required capabilities have no default; optional ones default to null,
// meaning "not supported", so a channel only names what it can do.
public interface IChannel
{
    ITodoDriver Todos { get; }

    ITodoTextInputDriver? TextInput => null;
}
