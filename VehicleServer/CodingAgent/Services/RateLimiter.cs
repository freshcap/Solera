using Microsoft.Extensions.Logging;

namespace CodingAgent.Services;

public interface IRateLimiter
{
    Task WaitIfNeeded();
    Task HandleRateLimitError(int retryAfterSeconds);
    void RecordApiCall();
}

public class RateLimiter : IRateLimiter
{
    private readonly ILogger<RateLimiter> _logger;
    private readonly int _maxCallsPerMinute;
    private readonly Queue<DateTime> _callTimestamps = new();
    private readonly object _lock = new();
    private DateTime? _rateLimitedUntil;

    public RateLimiter(ILogger<RateLimiter> logger, int maxCallsPerMinute = 50)
    {
        _logger = logger;
        _maxCallsPerMinute = maxCallsPerMinute;
    }

    public void RecordApiCall()
    {
        lock (_lock)
        {
            _callTimestamps.Enqueue(DateTime.Now);
        }
    }

    public async Task WaitIfNeeded()
    {
        // Check if we're in a rate-limited state
        if (_rateLimitedUntil.HasValue)
        {
            var waitTime = _rateLimitedUntil.Value - DateTime.Now;
            if (waitTime.TotalSeconds > 0)
            {
                _logger.LogWarning("Rate limited. Waiting {Seconds} seconds", (int)waitTime.TotalSeconds);
                await Task.Delay(waitTime);
            }
            _rateLimitedUntil = null;
        }

        TimeSpan? delayTime = null;
        int callCount = 0;

        // Calculate wait time inside lock
        lock (_lock)
        {
            var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
            while (_callTimestamps.Count > 0 && _callTimestamps.Peek() < oneMinuteAgo)
            {
                _callTimestamps.Dequeue();
            }

            // Check if we've hit the rate limit
            if (_callTimestamps.Count >= _maxCallsPerMinute)
            {
                var oldestCall = _callTimestamps.Peek();
                var waitUntil = oldestCall.AddMinutes(1);
                var waitTime = waitUntil - DateTime.Now;

                if (waitTime.TotalSeconds > 0)
                {
                    delayTime = waitTime;
                    callCount = _callTimestamps.Count;
                }
            }
        }

        // Await outside the lock
        if (delayTime.HasValue)
        {
            _logger.LogInformation(
                "Rate limit approaching ({Count}/{Max} calls). Waiting {Seconds} seconds",
                callCount, _maxCallsPerMinute, (int)delayTime.Value.TotalSeconds);

            await Task.Delay(delayTime.Value);

            // Clean up again after waiting
            lock (_lock)
            {
                var newOneMinuteAgo = DateTime.Now.AddMinutes(-1);
                while (_callTimestamps.Count > 0 && _callTimestamps.Peek() < newOneMinuteAgo)
                {
                    _callTimestamps.Dequeue();
                }
            }
        }
    }

    public async Task HandleRateLimitError(int retryAfterSeconds)
    {
        _logger.LogWarning("Received 429 Rate Limit error. Retry after: {Seconds} seconds", retryAfterSeconds);

        // Use exponential backoff if no retry-after is specified
        if (retryAfterSeconds <= 0)
        {
            retryAfterSeconds = CalculateExponentialBackoff();
        }

        _rateLimitedUntil = DateTime.Now.AddSeconds(retryAfterSeconds);

        _logger.LogInformation("Waiting {Seconds} seconds before retry", retryAfterSeconds);
        await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds));
    }

    private int CalculateExponentialBackoff()
    {
        // Simple exponential backoff: 1, 2, 4, 8, 16, 32, 60 (capped)
        var baseDelay = 1;
        var maxDelay = 60;

        lock (_lock)
        {
            var recentCalls = _callTimestamps.Count;
            var backoffMultiplier = Math.Min(recentCalls / 10, 5); // Cap at 5 doublings
            var delay = baseDelay * (int)Math.Pow(2, backoffMultiplier);

            return Math.Min(delay, maxDelay);
        }
    }
}
