namespace acceptance.Core.Dsl;

// Thrown when a Given step can't arrange what a spec needs. Its own type, so
// a report shows a broken precondition rather than the behaviour under test
// failing an assertion.
public sealed class GivenFailedException(string message) : Exception(message);
