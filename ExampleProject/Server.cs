using Rose.Net;

public class Server : GenericServer
{
    public Server()
    {
        OnClientConnect += OnConnection;
        OnClientRequest += OnRequest;
        OnClientClose += OnClose;
    }
    public void OnConnection(Client client)
    {
        Console.WriteLine($"Connection from {client.IP}");
    }
    public void OnRequest(Client client)
    {
        Console.WriteLine($"Request from {client.IP}: {client.Method} {client.Path} {client.Version}");
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