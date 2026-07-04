string filePath = "/usr/src/app/files/log.txt";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/", () => File.Exists(filePath) ? File.ReadAllText(filePath) : "no data yet");

app.Run();
