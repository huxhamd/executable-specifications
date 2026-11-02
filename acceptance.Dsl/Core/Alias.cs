namespace acceptance.Core.Dsl;

internal class Alias
{
    private const int SuffixLength = 7;   // "_" plus 6 characters
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..(SuffixLength - 1)];

    public string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name}_{_suffix}";

    // A value unique to this spec that the system sees as exactly `length`
    // characters, suffix included.
    public string OfLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, SuffixLength);
        return For(new string('a', length - SuffixLength));
    }
}
