namespace acceptance.Dsl;

public class Alias
{
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..6];

    public string For(string name) =>
        string.IsNullOrWhiteSpace(name) ? name : $"{name}_{_suffix}";
}