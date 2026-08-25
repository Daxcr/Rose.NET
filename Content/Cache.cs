using System.Collections.Concurrent;

namespace Rose.Net;

public static class Cache
{
    internal static ConcurrentDictionary<string, RoseFile> Data = new();
    internal static Task Session = null!;
    public static RoseFile GetFile(Client client)
    {
        CheckSession();
        if (Data.ContainsKey(client.Path!))
            return Data[client.Path!];

        RoseFile file = new()
        {
            Data = File.ReadAllBytes(client.Path!),
            Path = client.Path!,
            Added = (long)(DateTime.UtcNow - new DateTime(2000, 1, 1)).TotalMinutes
        };
        Data.TryAdd(client.Path!, file);
        return file;
    }
    public async static Task<RoseFile> GetFileAsync(Client client)
    {
        CheckSession();
        if (Data.ContainsKey(client.Path!))
            return Data[client.Path!];

        RoseFile file = new()
        {
            Data = await File.ReadAllBytesAsync(client.Path!),
            Path = client.Path!
        };
        Data.TryAdd(client.Path!, file);
        return file;
    }

    internal static void CheckSession()
    {
        if (Session == null)
            Session = Task.Run(RunSession);
    }

    public static void Clear() => Data = new();
    private static void RunSession()
    {
        long minutesSince2000 = (long)(DateTime.UtcNow - new DateTime(2000, 1, 1)).TotalMinutes;
        foreach (KeyValuePair<string, RoseFile> file in Data)
        {
            if (file.Value.Added + file.Value.TTL < minutesSince2000)
                Data.TryRemove(file);
        }
        Thread.Sleep(60);
    }
}