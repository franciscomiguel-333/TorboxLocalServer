using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Leer configuración desde appsettings.json (y permite override por variables de entorno)
var torboxApiKey = builder.Configuration["TorBox:ApiKey"];
var authUser = builder.Configuration["TorBox:AuthUser"] ?? "admin";
var authPass = builder.Configuration["TorBox:AuthPass"] ?? "adminadmin";
var port = builder.Configuration.GetValue<int>("TorBox:Port", 8000);

if (string.IsNullOrWhiteSpace(torboxApiKey))
{
    Console.WriteLine("[ERROR] Debes configurar tu TorBox API Key en appsettings.json (TorBox:ApiKey)");
    Console.WriteLine("Puedes obtenerla en https://torbox.app/settings");
    Console.WriteLine("Presiona cualquier tecla para salir...");
    Console.ReadKey();
    return;
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

var app = builder.Build();

const string TorboxBase = "https://api.torbox.app/v1/api";

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", torboxApiKey);

// ====== BASIC AUTH MIDDLEWARE ======
app.Use(async (context, next) =>
{
    string? authHeader = context.Request.Headers["Authorization"];
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
    {
        context.Response.Headers["WWW-Authenticate"] = "Basic";
        context.Response.StatusCode = 401;
        return;
    }

    var encoded = authHeader["Basic ".Length..].Trim();
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    var parts = decoded.Split(':', 2);

    if (parts.Length != 2 || parts[0] != authUser || parts[1] != authPass)
    {
        context.Response.StatusCode = 403;
        return;
    }

    await next();
});

// ====== ENDPOINT: INDEX ======
app.MapGet("/", async (HttpContext ctx) =>
{
    var listResp = await httpClient.GetAsync($"{TorboxBase}/torrents/mylist?bypass_cache=true");
    var listJson = await listResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(listJson);

    var files = new List<object>();

    foreach (var torrent in doc.RootElement.GetProperty("data").EnumerateArray())
    {
        var torrentId = torrent.GetProperty("id").GetInt64();
        if (!torrent.TryGetProperty("files", out var fileList)) continue;

        foreach (var file in fileList.EnumerateArray())
        {
            var fileId = file.GetProperty("id").GetInt64();
            var name = file.GetProperty("short_name").GetString();
            var size = file.GetProperty("size").GetInt64();

            if (name is null) continue;
            if (!name.EndsWith(".nsp") && !name.EndsWith(".nsz") &&
                !name.EndsWith(".xci") && !name.EndsWith(".xcz")) continue;

            files.Add(new
            {
                url = $"/torrents/{torrentId}/{fileId}#{Uri.EscapeDataString(name)}",
                size
            });
        }
    }

    return Results.Json(new { files, success = true });
});

// ====== ENDPOINT: DOWNLOAD CON RANGE ======
app.MapGet("/torrents/{torrentId}/{fileId}", async (HttpContext ctx, string torrentId, string fileId) =>
{
    var dlUrl = $"{TorboxBase}/torrents/requestdl?token={torboxApiKey}&torrent_id={torrentId}&file_id={fileId}";
    var dlResp = await httpClient.GetAsync(dlUrl);
    var dlJson = await dlResp.Content.ReadAsStringAsync();

    using var dlDoc = JsonDocument.Parse(dlJson);
    var realUrl = dlDoc.RootElement.GetProperty("data").GetString();

    if (string.IsNullOrEmpty(realUrl))
    {
        ctx.Response.StatusCode = 404;
        return;
    }

    var req = new HttpRequestMessage(HttpMethod.Get, realUrl);
    if (ctx.Request.Headers.TryGetValue("Range", out var rangeHeader))
        req.Headers.TryAddWithoutValidation("Range", rangeHeader.ToString());

    var cdnResp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

    ctx.Response.StatusCode = (int)cdnResp.StatusCode;
    ctx.Response.Headers["Accept-Ranges"] = "bytes";

    if (cdnResp.Content.Headers.ContentRange != null)
        ctx.Response.Headers["Content-Range"] = cdnResp.Content.Headers.ContentRange.ToString();

    if (cdnResp.Content.Headers.ContentLength.HasValue)
        ctx.Response.ContentLength = cdnResp.Content.Headers.ContentLength.Value;

    ctx.Response.ContentType = "application/octet-stream";

    await using var stream = await cdnResp.Content.ReadAsStreamAsync();
    await stream.CopyToAsync(ctx.Response.Body, bufferSize: 262144);
});

Console.WriteLine($"TorBox Tinfoil Server corriendo en el puerto {port}");
Console.WriteLine($"Usuario: {authUser}");

app.Run();