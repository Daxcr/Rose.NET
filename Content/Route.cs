using System.Diagnostics.CodeAnalysis;

namespace Rose.Net;

public struct Route
{
    public readonly required string Method { get; init; }
    public readonly required string Path { get; init; }
    public bool Enabled = true;
    public Func<Client, Task>? OnFire;
    public Route() { }
    internal static Route Make(string Method, string Path, Func<Client, Task> Handler)
    {
        Route route = new Route()
        {
            Method = Method,
            Path = Path
        };
        route.OnFire += Handler;
        return route;
    }
    public static Route Get(string Path, Func<Client, Task> Handler) => Make("GET", Path, Handler);
    public static Route Post(string Path, Func<Client, Task> Handler) => Make("POST", Path, Handler);
    public static Route Put(string Path, Func<Client, Task> Handler) => Make("PUT", Path, Handler);
    public static Route Patch(string Path, Func<Client, Task> Handler) => Make("PATCH", Path, Handler);
    public static Route Delete(string Path, Func<Client, Task> Handler) => Make("DELETE", Path, Handler);
    public static Route Head(string Path, Func<Client, Task> Handler) => Make("HEAD", Path, Handler);
    public static Route Options(string Path, Func<Client, Task> Handler) => Make("OPTIONS", Path, Handler);
    public static Route Connect(string Path, Func<Client, Task> Handler) => Make("CONNECT", Path, Handler);
    public static Route Trace(string Path, Func<Client, Task> Handler) => Make("TRACE", Path, Handler);

    /// <summary>
    /// Note: QUERY is fairly new and not widely adopted yet. Use with caution.
    /// </summary>
    [Obsolete("QUERY is fairly new and not widely adopted yet. Use with caution.")]
    public static Route Query(string Path, Func<Client, Task> Handler) => Make("QUERY", Path, Handler);
}