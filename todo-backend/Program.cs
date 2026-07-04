List<string> todos = new();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/todos", () => todos);

app.MapPost("/todos", (TodoRequest request) =>
{
    todos.Add(request.Content);
    return Results.Created();
});

app.Run();

record TodoRequest(string Content);
