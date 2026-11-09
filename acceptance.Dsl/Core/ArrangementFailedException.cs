namespace acceptance.Core.Dsl;

// Thrown when a step on a pinned channel (Api.) can't arrange what a spec
// needs. Its own type, so a report shows a broken arrangement rather than
// the behaviour under test failing an assertion.
public sealed class ArrangementFailedException(string message) : Exception(message);
