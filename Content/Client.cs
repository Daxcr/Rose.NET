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
    public bool RouteMatched { get; internal set; } = false;
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
        string root = System.IO.Path.GetFullPath("wwwroot");

        string combined = path[0] == '/' ? System.IO.Path.Combine(root, path.TrimStart('/')) : System.IO.Path.Combine(root, Path!, path);
        string fullpath = System.IO.Path.GetFullPath(combined);

        if (!fullpath.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            await Respond404();
            return;
        }

        path = System.IO.Path.Combine("wwwroot", path.TrimStart('/'));
        
        string? file = GetFileName(path);

        if (file == null)
        {
            await Respond404();
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
        return System.IO.Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            ".xml" => "text/xml",
            ".ics" => "text/calendar",
            ".md" => "text/markdown",
            ".rtf" => "text/rtf",
            ".tsv" => "text/tab-separated-values",
            ".js" or ".mjs" => "application/javascript",
            ".json" => "application/json",
            ".jsonld" => "application/ld+json",
            ".map" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".avif" => "image/avif",
            ".apng" => "image/apng",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".m4a" => "audio/mp4",
            ".opus" => "audio/opus",
            ".weba" => "audio/webm",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".ogv" => "video/ogg",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".mkv" => "video/x-matroska",
            ".flv" => "video/x-flv",
            ".wmv" => "video/x-ms-wmv",
            ".m4v" => "video/mp4",
            ".ts" => "video/mp2t",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".eot" => "application/vnd.ms-fontobject",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            ".epub" => "application/epub+zip",
            ".zip" => "application/zip",
            ".gz" => "application/gzip",
            ".tar" => "application/x-tar",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".bz2" => "application/x-bzip2",
            ".xz" => "application/x-xz",
            ".zst" => "application/zstd",
            ".wasm" => "application/wasm",
            ".yaml" or ".yml" => "application/yaml",
            ".toml" => "application/toml",
            ".sh" => "application/x-sh",
            ".sql" => "application/sql",
            ".webmanifest" => "application/manifest+json",
            ".atom" => "application/atom+xml",
            ".rss" => "application/rss+xml",
            _ => "application/octet-stream"
        };
    }
    public static string? GetFileName(string path)
    {
        if (File.Exists(path))
            return path;
        else if (File.Exists($"{path}.html"))
            return $"{path}.html";
        else if (File.Exists($"{path}/index.html"))
            return $"{path}/index.html";
        return null;
    }
    public bool IsRequestType(string type)
    {
        if (Headers == null) return false;
        if (!Headers.TryGetValue("Accept", out string? accept)) return false;
        return accept.Contains(type);
    }
}