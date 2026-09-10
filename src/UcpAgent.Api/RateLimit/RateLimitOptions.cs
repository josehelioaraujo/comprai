namespace UcpAgent.Api.RateLimit;

public class RateLimitOptions
{
    public FixedWindowOptions FixedWindow { get; set; } = new();
}

public class FixedWindowOptions
{
    public int PermitLimit   { get; set; } = 100;
    public int WindowSeconds { get; set; } = 10;
    public int QueueLimit    { get; set; } = 0;
}
