using Rose.Net;
using Rose.Net.Middleware.RateLimiting;

public class Server : GenericServer
{
    public Server(string name = "APP", bool silentInit = false)
        : base(name, silentInit)
    {
        OnClientConnect += OnConnection;
        OnClientRequest += OnRequest;
        OnClientClose += OnClose;

        AddRoute(Route.Get("/api/test", TestAPI));
        AddRoute(Route.Get("/api/*/helloworld/**", TestAPI));
        AddMiddleware<FixedWindow>();
    }
    public async Task TestAPI(Client client)
    {
        Debug.Log($"Route fired by {client.IP}");
        await client.Respond302("https://dax.cr/");
    }
    public async Task OnConnection(Client client) => Debug.Log($"Connection from {client.IP}");
    public async Task OnRequest(Client client)
    {
        Debug.Log($"Request from {client.IP}: {client.Method} {client.Path} {client.Version}");

        await ApplyRoutes(client);
        if (client.RouteMatched) return;

        await client.RespondStatic();
    }
    public async Task OnClose(Client client) => Debug.Log($"Client {client.IP} connection has closed");
}