string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.MapGet("/", () => "Server started in port " + port);

app.Lifetime.ApplicationStarted.Register(() =>
    Console.WriteLine($"Server started in port {port}"));

app.Run();
