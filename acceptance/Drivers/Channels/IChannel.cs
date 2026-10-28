namespace acceptance.Drivers.Channels;

// The capabilities one channel offers, split into one part per domain
// (Drivers/<Domain>/IChannel.<Domain>.cs). Required capabilities have no
// default; optional ones default to null, meaning "not supported".
public partial interface IChannel { }