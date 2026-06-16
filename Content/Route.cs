namespace Rose.Net;

public struct Route
{
    public readonly required string Method { get; init; }
    public readonly required string Path { get; init; }
    public bool Enabled = true;
    public Func<Client, string, string, Task>? OnFire;
    public Route() { }
    public static Route Get(string Path, Func<Client, string, string, Task> Handler)
    {
        Route route = new Route(){
            Method = "GET",
            Path = Path
        };
        route.OnFire += Handler;
        return route;
    }
    public static Route Post(string Path, Func<Client, string, string, Task> Handler)
    {
        Route route = new Route(){
            Method = "POST",
            Path = Path
        };
        route.OnFire += Handler;
        return route;
    }
}