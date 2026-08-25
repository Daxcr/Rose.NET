namespace Rose.Net;

public class RoseFile
{
    public byte[] Data = null!;
    public string Path = string.Empty;
    public uint TTL = 10;
    internal long Added;
}