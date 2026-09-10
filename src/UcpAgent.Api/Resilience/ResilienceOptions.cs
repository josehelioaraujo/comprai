namespace UcpAgent.Api.Resilience;

public class ResilienceOptions
{
    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1.0;
}

public class CircuitBreakerOptions
{
    public double FailureRatio { get; set; } = 0.5;
    public int MinimumThroughput { get; set; } = 10;
    public int SamplingDurationSeconds { get; set; } = 30;
    public int BreakDurationSeconds { get; set; } = 15;
}

public class TimeoutOptions
{
    public int TimeoutSeconds { get; set; } = 5;
}
