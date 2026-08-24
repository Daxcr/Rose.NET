namespace Rose.Net;

public static class Debug
{
    public static void Log(string message, string identifier = "APP")
    {
        if (identifier == "ROSE")
            identifier = "APP";
        Console.WriteLine($"[ {identifier} ] {DateTime.Now} {message}");
    }
    internal static void InternalLog(string message) => Console.WriteLine($"[ ROSE ] {DateTime.Now} {message}");
}