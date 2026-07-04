string randomString = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/", () =>
{
    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
    return $"{timestamp}: {randomString}";
});

_ = Task.Run(async () =>
{
    while (true)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
        Console.WriteLine($"{timestamp}: {randomString}");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

app.Run();
