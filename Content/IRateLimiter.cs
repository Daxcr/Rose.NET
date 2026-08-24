namespace Rose.Net;

public interface IRateLimiter
{
    public virtual bool AmIRateLimited() { return false; }
}