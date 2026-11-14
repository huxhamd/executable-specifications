namespace acceptance.Core.Dsl;

public sealed class InvalidSpecException(string problem) : Exception($"The spec is wrong: {problem}");
