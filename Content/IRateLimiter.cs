namespace Rose.Net;

public interface IRateLimiter
{
    public enum LimitType { Fixed, Sliding, TokenBucket }
    public virtual bool AmIRateLimited() { return false; }
}