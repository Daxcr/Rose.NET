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
    public void RouteFired(Client client, string Method, string Path)
    {
        Console.WriteLine($"Route fired by {client.IP}");
        _ = client.RespondStatic("daxpfp.png");
    }
    public void OnConnection(Client client)
    {
        Console.WriteLine($"Connection from {client.IP}");
    }
    public void OnRequest(Client client)
    {
        Console.WriteLine($"Request from {client.IP}: {client.Method} {client.Path} {client.Version}");

        ApplyRoutes(client);
        if (client.RouteMatched) return;

        if (client.IsRequestType("text/html"))
            _ = client.RespondStatic("index.html");
        else
            _ = client.RespondStatic();
    }
    public void OnClose(Client client)
    {
        Console.WriteLine($"Client {client.IP} has expired");
    }
}