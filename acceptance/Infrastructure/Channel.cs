namespace acceptance.Infrastructure;

public enum Channel { Api, Ui }

// The channel this run drives the system through, from ACCEPTANCE_CHANNEL.
public static class AcceptanceChannel
{
    public static Channel Current
    {
        get
        {
            var value = Environment
                .GetEnvironmentVariable("ACCEPTANCE_CHANNEL")
                ?.Trim().ToLowerInvariant();
            return value switch
            {
                "api" => Channel.Api,
                "ui" => Channel.Ui,
                null or "" => throw new InvalidOperationException(
                    "Set ACCEPTANCE_CHANNEL to 'api' or 'ui' before running the acceptance tests."),
                _ => throw new InvalidOperationException(
                    $"Unknown ACCEPTANCE_CHANNEL '{value}'. Use 'api' or 'ui'.")
            };
        }
    }
}
