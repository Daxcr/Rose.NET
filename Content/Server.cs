using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rose.Net;

public class GenericServer
{
    public required int Port { get; init; }
    public required string Host { get; init; }
    public required int MaxRequestSize { get; init; }
    public bool AcceptingConnections
    {
        get;
        set
        {
            field = value;
            if (value)
            {
                Ip ??= IPAddress.Parse(Host);
                Listener ??= new TcpListener(IPAddress.Parse(Host), Port);
                Listener.Start();
                Runtime = Run();
            } else if (Listener != null) Listener.Stop();
        }
    }
    public Task? Runtime;
    IPAddress? Ip;
    TcpListener? Listener;
    public event Func<Client, Task>? OnClientConnect;
    public event Func<Client, Task>? OnClientRequest;
    public event Func<Client, Task>? OnClientClose;
    public Dictionary<string, Route> Routes = new();
    public bool RouteMatched { get; internal set; } = false;
    public void AddRoute(Route route)
    {
        Routes.Add($"{route.Path}{route.Method}", route);
    }
    public async Task ApplyRoutes(Client client)
    {
        if (Routes.TryGetValue($"{client.Path}{client.Method}", out Route route) && route.Enabled)
        {
            client.RouteMatched = true;
            if (route.OnFire != null)
                await route.OnFire.Invoke(client, client.Method!, client.Path!);
        }
    }
    public async Task Run()
    {
        while (AcceptingConnections)
        {
            TcpClient tcpClient = await Listener!.AcceptTcpClientAsync();
            _ = Task.Run(async () =>
            {
                Client client = new Client(tcpClient);
                client.Parent = this;
                await OnClientConnect?.Invoke(client)!;

                NetworkStream stream = tcpClient.GetStream();
                client.Stream = stream;
                try
                {
                    while (true)
                    {
                        byte[] buffer = new byte[4096];
                        string request = "";
                        int totalBytesRead = 0;
                        
                        while (!request.Contains("\r\n\r\n"))
                        {
                            int bytesRead = await stream.ReadAsync(buffer);

                            if (bytesRead == 0) return;
                            
                            totalBytesRead += bytesRead;
                            request += Encoding.UTF8.GetString(buffer, 0, bytesRead);

                            if (totalBytesRead > MaxRequestSize)
                            {
                                string response = "HTTP/1.1 413 Content Too Large\r\n\r\n";
                                await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
                                tcpClient.Close();
                                return;
                            }
                        }
                        client.RouteMatched = false;
                        client.Request = request;
                        await OnClientRequest?.Invoke(client)!;
                    }

                } catch { } finally
                {
                    await OnClientClose?.Invoke(client)!;
                }
            });
        }
    }
}