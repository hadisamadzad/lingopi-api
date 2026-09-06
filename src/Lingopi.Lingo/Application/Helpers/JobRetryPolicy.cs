namespace Lingopi.Lingo.Application.Helpers;

public static class JobRetryPolicy
{
    public static TimeSpan CalculateDelay(int attemptCount, TimeSpan initialDelay, TimeSpan maxDelay)
    {
        var exponent = Math.Max(0, attemptCount - 1);
        var delaySeconds = Math.Min(
            maxDelay.TotalSeconds,
            initialDelay.TotalSeconds * Math.Pow(2, exponent));

        return TimeSpan.FromSeconds(delaySeconds);
    }
}
