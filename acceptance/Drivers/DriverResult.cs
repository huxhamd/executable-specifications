namespace acceptance.Drivers;

public record DriverResult(bool Succeeded, string Error = "")
{
    public static readonly DriverResult Ok = new(true);
    public static DriverResult Failed(string error) =>
        new(false, error);

    public void ShouldSucceed(string because) =>
        Assert.True(Succeeded, $"{because}. The system said: {Error}");


    // Operations with no result

    public static Task<DriverResult> TryCatchAsync(
        Func<Task> operation,
        string? error = null)
        => TryCatchAsync(operation, exception => error ?? exception.Message);

    public static Task<DriverResult> TryCatchAsync<TException>(
        Func<Task> operation,
        string? error = null)
        where TException : Exception
        => TryCatchAsync(operation,
            exception => exception is TException ? error ?? exception.Message : null);

    public static Task<DriverResult> TryCatchAsync(
        Func<Task> operation,
        Func<Exception, string?> mapException)
        => TryCatchAsync(async () => { await operation(); return Ok; }, mapException);

    // Operations that already produce a DriverResult

    public static Task<DriverResult> TryCatchAsync(
        Func<Task<DriverResult>> operation,
        string? error = null)
        => TryCatchAsync(operation, exception => error ?? exception.Message);

    public static Task<DriverResult> TryCatchAsync<TException>(
        Func<Task<DriverResult>> operation,
        string? error = null)
        where TException : Exception
        => TryCatchAsync(operation,
            exception => exception is TException ? error ?? exception.Message : null);

    public static async Task<DriverResult> TryCatchAsync(
        Func<Task<DriverResult>> operation,
        Func<Exception, string?> mapException)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (mapException(exception) is { } error)
        {
            return Failed(error);
        }
    }
}