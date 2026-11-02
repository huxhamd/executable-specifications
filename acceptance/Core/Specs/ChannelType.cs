namespace acceptance.Core.Specs;

public enum ChannelType { Api, Ui }

// The channel this run drives the system through, from ACCEPTANCE_CHANNEL.
public static class AcceptanceChannel
{
    public static ChannelType Current
    {
        get
        {
            var value = Environment
                .GetEnvironmentVariable("ACCEPTANCE_CHANNEL")
                ?.Trim().ToLowerInvariant();
            return value switch
            {
                "api" => ChannelType.Api,
                "ui" => ChannelType.Ui,
                null or "" => throw new InvalidOperationException(
                    "Set ACCEPTANCE_CHANNEL to 'api' or 'ui' before running the acceptance tests."),
                _ => throw new InvalidOperationException(
                    $"Unknown ACCEPTANCE_CHANNEL '{value}'. Use 'api' or 'ui'.")
            };
        }
    }
}
