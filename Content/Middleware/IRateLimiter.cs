namespace Rose.Net.Middleware;

public interface IRateLimiter : IMiddleware, IPrivileged
{
    void IMiddleware.Register() => Privilege = 0;
    void IMiddleware.Apply(Client client) => AmIRateLimited(client.IP);
    public virtual bool AmIRateLimited(string IP) => false;
}