namespace acceptance.Dsl;

public static class Alias
{
    private readonly static string _suffix = Guid.NewGuid().ToString("N")[..6];

    public static string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name}_{_suffix}";
}