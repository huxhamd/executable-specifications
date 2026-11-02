namespace acceptance.Core.Specs;

// The channels a spec can run through. scripts/run-acceptance-tests.ps1 keeps
// its own list of channel names: add or rename a channel in both places.
public enum ChannelType { Api, Ui }

// The channel this run drives the system through, from ACCEPTANCE_CHANNEL:
// a ChannelType name, in any case.
public static class AcceptanceChannel
{
    public static ChannelType Current
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("ACCEPTANCE_CHANNEL")?.Trim();
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException(
                    $"Set ACCEPTANCE_CHANNEL to {Choices} before running the acceptance tests.");

            // Matched by name rather than Enum.TryParse, which would also accept
            // numbers ("0") and lists of flags ("Api, Ui").
            var name = Enum.GetNames<ChannelType>()
                .SingleOrDefault(candidate => candidate.Equals(value, StringComparison.OrdinalIgnoreCase));
            return name is not null
                ? Enum.Parse<ChannelType>(name)
                : throw new InvalidOperationException(
                    $"Unknown ACCEPTANCE_CHANNEL '{value}'. Use {Choices}.");
        }
    }

    private static string Choices =>
        string.Join(" or ", Enum.GetNames<ChannelType>().Select(name => $"'{name.ToLowerInvariant()}'"));
}
