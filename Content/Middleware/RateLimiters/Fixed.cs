namespace Rose.Net.Middleware.RateLimiting;

public class FixedWindow : IRateLimiter
{
    public uint Privilege { get; set; }
}