namespace Rose.Net.Middleware;

public interface IMiddleware
{
    internal virtual void Register() { }
    internal virtual void Apply(Client client) { }
}