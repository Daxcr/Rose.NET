using Rose.Net;

Server server = new Server
{
    Port = 5555,
    Host = "127.0.0.1",
    MaxRequestSize = 1024 * 1024
};
server.AcceptingConnections = true;

Thread.Sleep(-1);