string filePath = "/usr/src/app/shared/pingpong.txt";
int counter = File.Exists(filePath) && int.TryParse(File.ReadAllText(filePath), out int savedCounter)
    ? savedCounter + 1
    : 0;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/pingpong", () =>
{
    string response = $"pong {counter}";
    File.WriteAllText(filePath, counter.ToString());
    counter++;
    return response;
});

app.Run();
