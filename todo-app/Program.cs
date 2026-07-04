string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
string imagePath = "/usr/src/app/images/image.jpg";
TimeSpan cacheDuration = TimeSpan.FromMinutes(10);
HttpClient httpClient = new HttpClient();
bool isFetching = false;

byte[]? cachedImage = File.Exists(imagePath) ? File.ReadAllBytes(imagePath) : null;
DateTime lastFetched = File.Exists(imagePath) ? File.GetLastWriteTimeUtc(imagePath) : DateTime.MinValue;

async Task FetchNewImage()
{
    byte[] bytes = await httpClient.GetByteArrayAsync("https://picsum.photos/1200");
    File.WriteAllBytes(imagePath, bytes);
    cachedImage = bytes;
    lastFetched = DateTime.UtcNow;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var indexPage = () => Results.Content(
    $$"""
    <html>
    <head>
        <style>
            body { text-align: center; }
            img { max-width: 300px; }
        </style>
    </head>
    <body>
        <p>Server started in port {{port}}</p>
        <img src="/todos/image" />
        <h1>Todos</h1>
        <ul>
            <li>Buy groceries</li>
            <li>Learn Kubernetes</li>
        </ul>
        <input type="text" maxlength="140" />
        <button type="button">Send</button>
    </body>
    </html>
    """,
    "text/html");

app.MapGet("/", indexPage);
app.MapGet("/todos", indexPage);

var imageEndpoint = async () =>
{
    if (cachedImage == null)
    {
        await FetchNewImage();
    }
    else if (DateTime.UtcNow - lastFetched >= cacheDuration && !isFetching)
    {
        isFetching = true;
        _ = Task.Run(async () =>
        {
            await FetchNewImage();
            isFetching = false;
        });
    }

    return Results.File(cachedImage!, "image/jpeg");
};

app.MapGet("/image", imageEndpoint);
app.MapGet("/todos/image", imageEndpoint);

app.MapGet("/kill", () =>
{
    Environment.Exit(1);
    return Results.Ok();
});

app.Lifetime.ApplicationStarted.Register(() =>
    Console.WriteLine($"Server started in port {port}"));

app.Run();
