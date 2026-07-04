string logFilePath = "/usr/src/app/files/log.txt";
string pingPongFilePath = "/usr/src/app/shared/pingpong.txt";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/", () =>
{
    string logLine = "no data yet";
    if (File.Exists(logFilePath))
    {
        var lines = File.ReadAllLines(logFilePath);
        if (lines.Length > 0)
        {
            logLine = lines[^1];
        }
    }

    string pingCount = File.Exists(pingPongFilePath) ? File.ReadAllText(pingPongFilePath) : "0";

    return $"{logLine}. Ping / Pongs: {pingCount}";
});

app.Run();
