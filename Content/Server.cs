using System.Net;
using System.Net.Sockets;
using System.Text;
using Rose.Net.Middleware;

namespace Rose.Net;

public class GenericServer
{
    public string Name;
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
                Debug.InternalLog($"Server {Name} listening on {Host}:{Port}");
                Runtime = Run();
            } else if (Listener != null) Listener.Stop();
        }
    }
    internal Task? Runtime;
    IPAddress? Ip;
    TcpListener? Listener;
    /// <summary>When a client first connects, this fires.</summary>
    public event Func<Client, Task>? OnClientConnect;
    /// <summary>Once a client's request is parsed, this fires.</summary>
    public event Func<Client, Task>? OnClientRequest;
    /// <summary>After a connection is closed, this fires.</summary>
    public event Func<Client, Task>? OnClientClose;
    public Dictionary<string, Route> Routes = new();
    /// <summary>Middleware added will be required to be called manually. Standard middleware is intended for internal middleware. External libraries should use privileged middleware instead.</summary>
    public List<IMiddleware> StandardMiddleware = new();
    /// <summary>Middleware added will automatically run before OnClientRequest fires.</summary>
    public List<IPrivileged> PrivilegedMiddleware = new();
    public GenericServer(string name = "APP", bool silentInit = false)
    {
        Name = name;
        if (!silentInit)
            Debug.InternalLog($"New Server: {Name}");
    }
    /// <summary>
    /// Adds a route. Wildcard supported.
    /// <para>Example: "/api/users/*/bio" will allow client paths such as "/api/users/dax/bio"</para>
    /// <para>Example: "/cdn/**" will allow client paths such as "/cdn/files/dax/image.png"</para>
    /// </summary>
    /// <param name="route">It is recommended to use Route.Get/Route.Post/etc instead of manually creating a Route object.</param>
    public void AddRoute(Route route)
    {
        Debug.InternalLog($"Added route for {Name}: {route.Path} ({route.Method})");
        Routes.Add($"{route.Path}{route.Method}", route);
    }
    public void AddMiddleware<Type>(string path = "*") where Type : new()
    {
        Type type = new Type();
        if (type is not IMiddleware)
            return;

        IMiddleware middleware = (type as IMiddleware)!;
        
        if (middleware is IPrivileged)
        {
            PrivilegedMiddleware.Add((middleware as IPrivileged)!);
            PrivilegedMiddleware.OrderBy(md => md.Privilege).ToList();
        } else
        {
            StandardMiddleware.Add(middleware);
        }
        Debug.InternalLog($"Added middleware {middleware.GetType()}");
    }
    /// <summary>
    /// Applies routes. Note: this does not stop OnClientRequest from firing. Returning if RouteMatched == true is recommended.
    /// </summary>
    /// <param name="client">The client, of course.</param>
    public async Task ApplyRoutes(Client client)
    {
        if (Routes.TryGetValue($"{client.Path}{client.Method}", out Route routeA) && routeA.Enabled)
        {
            client.RouteMatched = true;
            if (routeA.OnFire != null)
                await routeA.OnFire.Invoke(client);
            return;
        }
        string[] required = client.Path!.TrimEnd('/').Split('/');
        foreach (Route routeB in Routes.Values)
        {
            if (!routeB.Path.Contains("*") || routeB.Method != client.Method)
                continue;
                
            string[] possible = routeB.Path.TrimEnd('/').Split("/");
            if (possible[possible.Length - 1] != "**" || possible.Length > required.Length)
                continue;

            int index = 0;
            foreach (string val in required)
            {
                if (val != possible[index] && possible[index] != "*" && possible[index] != "**")
                    break;

                if (index == possible.Length - 1 || possible[index] == "**")
                {
                    client.RouteMatched = true;
                    if (routeB.OnFire != null)
                        await routeB.OnFire.Invoke(client);
                    return;
                }

                index += 1;
            }
        }
    }
    internal async Task Run()
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
                                await client.Respond413();
                                tcpClient.Close();
                                return;
                            }
                        }
                        client.RouteMatched = false;
                        client.Request = request;
                        await OnClientRequest?.Invoke(client)!;
                    }

                } catch
                {
                    await client.Respond500();
                } finally
                {
                    await OnClientClose?.Invoke(client)!;
                }
            });
        }
    }
}