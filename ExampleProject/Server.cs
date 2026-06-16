using Rose.Net;

public class Server : GenericServer
{
    public Server()
    {
        OnClientConnect += OnConnection;
        OnClientRequest += OnRequest;
        OnClientClose += OnClose;

        AddRoute(Route.Get("/api/test", RouteFired));
    }
    public async Task RouteFired(Client client, string Method, string Path)
    {
        Console.WriteLine($"Route fired by {client.IP}");
        await client.Respond302("https://dax.cr/");
    }
    public async Task OnConnection(Client client)
    {
        Console.WriteLine($"Connection from {client.IP}");
    }
    public async Task OnRequest(Client client)
    {
        Console.WriteLine($"Request from {client.IP}: {client.Method} {client.Path} {client.Version}");

        await ApplyRoutes(client);
        if (client.RouteMatched) return;

        await client.RespondStatic();
    }
    public async Task OnClose(Client client)
    {
        Console.WriteLine($"Client {client.IP} has expired");
    }
}