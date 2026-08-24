Server server = new Server("TestApp")
{
    Port = 5555,
    Host = "127.0.0.1",
    MaxRequestSize = 1024 * 1024 * 3,
};
server.AcceptingConnections = true;

Thread.Sleep(-1);