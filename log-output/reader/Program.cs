string logFilePath = "/usr/src/app/files/log.txt";
string infoFilePath = "/usr/src/app/config/information.txt";
HttpClient httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/healthz", async () =>
{
    try
    {
        var response = await httpClient.GetAsync("http://pingpong.exercises.svc.cluster.local/count");
        return response.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode(500);
    }
    catch
    {
        return Results.StatusCode(500);
    }
});

app.MapGet("/", async () =>
{
    string fileContent = File.Exists(infoFilePath) ? File.ReadAllText(infoFilePath).Trim() : "";
    string message = Environment.GetEnvironmentVariable("MESSAGE") ?? "";

    string logLine = "no data yet";
    if (File.Exists(logFilePath))
    {
        var lines = File.ReadAllLines(logFilePath);
        if (lines.Length > 0)
        {
            logLine = lines[^1];
        }
    }

    string pingCount = await httpClient.GetStringAsync("http://pingpong.exercises.svc.cluster.local/count");

    return $"file content: {fileContent}\nenv variable: MESSAGE={message}\n{logLine}. Ping / Pongs: {pingCount}";
});

app.Run();
