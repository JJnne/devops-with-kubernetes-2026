using System.Text.Json;

string port = Environment.GetEnvironmentVariable("PORT")!;
string imagePath = Environment.GetEnvironmentVariable("IMAGE_PATH")!;
TimeSpan cacheDuration = TimeSpan.FromMinutes(double.Parse(Environment.GetEnvironmentVariable("IMAGE_CACHE_MINUTES")!));
string imageSourceUrl = Environment.GetEnvironmentVariable("IMAGE_SOURCE_URL")!;
string todoBackendUrl = Environment.GetEnvironmentVariable("TODO_BACKEND_URL")!;
HttpClient httpClient = new HttpClient();
bool isFetching = false;

byte[]? cachedImage = File.Exists(imagePath) ? File.ReadAllBytes(imagePath) : null;
DateTime lastFetched = File.Exists(imagePath) ? File.GetLastWriteTimeUtc(imagePath) : DateTime.MinValue;

async Task FetchNewImage()
{
    byte[] bytes = await httpClient.GetByteArrayAsync(imageSourceUrl);
    File.WriteAllBytes(imagePath, bytes);
    cachedImage = bytes;
    lastFetched = DateTime.UtcNow;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var indexPage = async () =>
{
    List<TodoDto>? todos;
    try
    {
        todos = await httpClient.GetFromJsonAsync<List<TodoDto>>(
            $"{todoBackendUrl}/todos",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        todos = null;
    }

    if (todos == null)
    {
        return Results.Content(
            $$"""
            <html>
            <head>
                <meta http-equiv="refresh" content="3">
                <style>
                    body { text-align: center; }
                </style>
            </head>
            <body>
                <p>Server started in port {{port}}</p>
                <h1>The app is broken!</h1>
                <p>Waiting for it to recover...</p>
            </body>
            </html>
            """,
            "text/html",
            statusCode: 500);
    }

    string todoItems = string.Concat(todos.Select(todo => $$"""

                <li>
                    <form action="/todos/{{todo.Id}}/done" method="post" style="display:inline">
                        <input type="hidden" name="done" value="{{(!todo.Done).ToString().ToLower()}}" />
                        <input type="checkbox" {{(todo.Done ? "checked" : "")}} onchange="this.form.submit()" />
                        <span style="text-decoration: {{(todo.Done ? "line-through" : "none")}}">{{todo.Content}}</span>
                    </form>
                </li>
"""));

    return Results.Content(
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
            <form action="/todos" method="post">
                <input type="text" name="content" maxlength="140" required />
                <button type="submit">Send</button>
            </form>
            <ul>{{todoItems}}
            </ul>
            <form action="/todos/break" method="post">
                <button type="submit">Break the app</button>
            </form>
        </body>
        </html>
        """,
        "text/html");
};

app.MapGet("/", indexPage);
app.MapGet("/todos", indexPage);

app.MapGet("/healthz", async () =>
{
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var response = await httpClient.GetAsync($"{todoBackendUrl}/healthz", cts.Token);
        return response.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode(500);
    }
    catch
    {
        return Results.StatusCode(500);
    }
});

var breakApp = async () =>
{
    await httpClient.PostAsync($"{todoBackendUrl}/break", null);
    return Results.Redirect("/todos");
};

app.MapPost("/break", breakApp);
app.MapPost("/todos/break", breakApp);

var createTodo = async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    string content = form["content"].ToString();
    await httpClient.PostAsJsonAsync($"{todoBackendUrl}/todos", new { content });
    return Results.Redirect("/todos");
};

app.MapPost("/", createTodo);
app.MapPost("/todos", createTodo);

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

app.MapPost("/todos/{id}/done", async (int id, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    bool done = form["done"] == "true";
    await httpClient.PutAsJsonAsync($"{todoBackendUrl}/todos/{id}", new { done });
    return Results.Redirect("/todos");
});

app.Lifetime.ApplicationStarted.Register(() =>
    Console.WriteLine($"Server started in port {port}"));

app.Run();

record TodoDto(int Id, string Content, bool Done);
