int counter = 0;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/pingpong", () => $"pong {counter++}");

app.Run();
