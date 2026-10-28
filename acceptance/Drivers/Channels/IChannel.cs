namespace acceptance.Drivers.Channels;

// The capabilities one channel offers, split into one part per domain
// (Drivers/<Domain>/IChannel.<Domain>.cs). Required capabilities have no
// default; optional ones default to null, meaning "not supported".
//
// Every part of IChannel, ApiChannel and UiChannel must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
public partial interface IChannel;
