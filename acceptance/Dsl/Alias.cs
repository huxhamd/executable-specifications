namespace acceptance.Dsl;

public class Alias
{
    public static readonly int SuffixLength = 7;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..(SuffixLength-1)];

    public string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name}_{_suffix}";
}