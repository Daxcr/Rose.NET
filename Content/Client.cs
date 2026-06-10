using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rose.Net;

public class Client
{
    TcpClient Socket;
    public string? Path;
    public string? Method;
    public string? Version;
    public Dictionary<string, string>? Headers;
    public string? Body;
    public string IP;
    public GenericServer? Parent;
    public NetworkStream? Stream;
    public string? Request
    {
        get;
        set
        {
            field = value;
            RequestRecieved(value!);
        }
    }
    public Client(TcpClient Socket)
    {
        this.Socket = Socket;
        IP = ((IPEndPoint)Socket.Client.RemoteEndPoint!).Address.ToString();
    }
    public void RequestRecieved(string request)
    {
        string[] lines = request.Split("\r\n");
        string[] parts = lines[0].Split(" ");
        
        Method = parts[0];
        Path = parts[1];
        Version = parts[2];
        Headers = new Dictionary<string, string>();
        int index = 1;
        while (index < lines.Length && lines[index] != "")
        {
            string[] header = lines[index].Split(": ", 2);
            if (header.Length == 2)
                Headers[header[0]] = header[1];
            index += 1;
        }

        Body = index + 1 < lines.Length ? string.Join("\r\n", lines[(index + 1)..]) : "";
    }
    public void Close() => Socket.Close();

    public async Task RespondStatic() => await RespondStatic(Path!);
    public async Task RespondStatic(string path)
    {
        string? file = GetFileName(path);

        if (file == null)
        {
            await Stream!.WriteAsync(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n\r\n"));
            return;
        }
        string contentType = GetContentType(file);
        string headers = $"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {new FileInfo(file).Length}\r\n\r\n";

        await Stream!.WriteAsync(Encoding.UTF8.GetBytes(headers));
        using FileStream filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        await filestream.CopyToAsync(Stream!);
    }
    public async Task Respond200(string body = "", string contentType = "text/html") => await RespondWithBody(200, "OK", body, contentType);
    public async Task Respond201(string body = "", string contentType = "text/html") => await RespondWithBody(201, "Created", body, contentType);
    public async Task Respond204() => await RespondNoBody(204, "No Content");

    public async Task Respond301(string location) => await RespondRedirect(301, "Moved Permanently", location);
    public async Task Respond302(string location) => await RespondRedirect(302, "Found", location);
    public async Task Respond304() => await RespondNoBody(304, "Not Modified");

    public async Task Respond400(string body = "") => await RespondWithBody(400, "Bad Request", body);
    public async Task Respond401(string body = "") => await RespondWithBody(401, "Unauthorized", body);
    public async Task Respond403(string body = "") => await RespondWithBody(403, "Forbidden", body);
    public async Task Respond404(string body = "") => await RespondWithBody(404, "Not Found", body);
    public async Task Respond405(string body = "") => await RespondWithBody(405, "Method Not Allowed", body);
    public async Task Respond408(string body = "") => await RespondWithBody(408, "Request Timeout", body);
    public async Task Respond409(string body = "") => await RespondWithBody(409, "Conflict", body);
    public async Task Respond410(string body = "") => await RespondWithBody(410, "Gone", body);
    public async Task Respond413(string body = "") => await RespondWithBody(413, "Content Too Large", body);
    public async Task Respond422(string body = "") => await RespondWithBody(422, "Unprocessable Entity", body);
    public async Task Respond429(string body = "") => await RespondWithBody(429, "Too Many Requests", body);
    public async Task Respond500(string body = "") => await RespondWithBody(500, "Internal Server Error", body);
    public async Task Respond501(string body = "") => await RespondWithBody(501, "Not Implemented", body);
    public async Task Respond502(string body = "") => await RespondWithBody(502, "Bad Gateway", body);
    public async Task Respond503(string body = "") => await RespondWithBody(503, "Service Unavailable", body);
    public async Task Respond504(string body = "") => await RespondWithBody(504, "Gateway Timeout", body);
    private async Task RespondNoBody(int code, string status)
        => await Stream!.WriteAsync(Encoding.UTF8.GetBytes($"HTTP/1.1 {code} {status}\r\nContent-Length: 0\r\n\r\n"));

    private async Task RespondWithBody(int code, string status, string body, string contentType = "text/plain")
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        string headers = $"HTTP/1.1 {code} {status}\r\nContent-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\n\r\n";
        await Stream!.WriteAsync(Encoding.UTF8.GetBytes(headers));
        if (bytes.Length > 0)
            await Stream!.WriteAsync(bytes);
    }

    private async Task RespondRedirect(int code, string status, string location) => await Stream!.WriteAsync(Encoding.UTF8.GetBytes($"HTTP/1.1 {code} {status}\r\nLocation: {location}\r\nContent-Length: 0\r\n\r\n"));

    public static string GetContentType(string path)
    {
        return System.IO.Path.GetExtension(path) switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
    public static string GetFileName(string path)
    {
        if (File.Exists($"wwwroot/{path}"))
            return $"wwwroot/{path}";
        else if (File.Exists($"wwwroot/{path}.html"))
            return $"wwwroot/{path}.html";
        else if (File.Exists($"wwwroot/{path}/index.html"))
            return $"wwwroot/{path}/index.html";
        
        return null!;
    }
    public bool IsRequestType(string type)
    {
        if (Headers == null) return false;
        if (Headers!.TryGetValue("Accept", out string? accept)) return false;
        return accept!.Contains(type);
    }
}