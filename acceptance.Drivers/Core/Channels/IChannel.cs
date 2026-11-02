namespace acceptance.Core.Channels;

// The capabilities one channel offers, split into one part per domain
// (<Domain>/Channels/IChannel.<Domain>.cs). Required capabilities have no
// default; optional ones default to null, meaning "not supported". Disposing
// a channel closes whatever it opened.
//
// Every part of IChannel, ApiChannel and UiChannel must be declared in this
// namespace, whatever folder it lives in. A part in another namespace is
// silently a different type.
internal partial interface IChannel : IAsyncDisposable;
