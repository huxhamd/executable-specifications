namespace acceptance.Core.Dsl;

internal sealed class Vocabulary<T>(string meaning)
{
    private readonly OrderedDictionary<string, T> _words = new(StringComparer.Ordinal);

    public T this[string word]
    {
        init => _words.Add(word, value);
    }

    public T Translate(string word) =>
        _words.TryGetValue(word, out var value)
            ? value
            : throw new InvalidSpecException(
                $"'{word}' is not a recognised {meaning}. Use one of: {string.Join(", ", _words.Keys)}.");
}
