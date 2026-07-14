using System.Text.Json;
using NATS.Client.Core;
using Npgsql;

string port = Environment.GetEnvironmentVariable("PORT")!;
string host = Environment.GetEnvironmentVariable("POSTGRES_HOST")!;
string dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT")!;
string database = Environment.GetEnvironmentVariable("POSTGRES_DB")!;
string username = Environment.GetEnvironmentVariable("POSTGRES_USER")!;
string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")!;
string connectionString = $"Host={host};Port={dbPort};Database={database};Username={username};Password={password}";

string natsUrl = Environment.GetEnvironmentVariable("NATS_URL")!;
string natsSubject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "todo_status";
await using var nats = new NatsConnection(new NatsOpts { Url = natsUrl });

async Task PublishTodoStatus(string eventName, int id, string? content, bool? done)
{
    string payload = JsonSerializer.Serialize(new TodoStatusMessage(eventName, id, content, done));
    await nats.PublishAsync(natsSubject, payload);
}

bool isHealthy = true;

_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            await using var initConn = new NpgsqlConnection(connectionString);
            await initConn.OpenAsync();
            await using var createCmd = initConn.CreateCommand();
            createCmd.CommandText = "CREATE TABLE IF NOT EXISTS todos (id SERIAL PRIMARY KEY, content TEXT NOT NULL)";
            await createCmd.ExecuteNonQueryAsync();
            await using var alterCmd = initConn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE todos ADD COLUMN IF NOT EXISTS done BOOLEAN NOT NULL DEFAULT FALSE";
            await alterCmd.ExecuteNonQueryAsync();
            break;
        }
        catch
        {
            await Task.Delay(2000);
        }
    }
});

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
var app = builder.Build();

app.MapGet("/healthz", async () =>
{
    if (!isHealthy)
    {
        return Results.Json(new { status = "unhealthy" }, statusCode: 500);
    }

    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        return Results.Json(new { status = "ok" });
    }
    catch
    {
        return Results.Json(new { status = "unhealthy" }, statusCode: 500);
    }
});

app.MapPost("/break", () =>
{
    isHealthy = false;
    return Results.Ok();
});

app.MapGet("/todos", async () =>
{
    if (!isHealthy)
    {
        return Results.StatusCode(500);
    }

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, content, done FROM todos ORDER BY id";
    await using var reader = await cmd.ExecuteReaderAsync();

    List<TodoDto> todos = new();
    while (await reader.ReadAsync())
    {
        todos.Add(new TodoDto(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
    }

    return Results.Ok(todos);
});

app.MapPost("/todos", async (TodoRequest request, ILogger<Program> logger) =>
{
    if (!isHealthy)
    {
        return Results.StatusCode(500);
    }

    logger.LogInformation("Received todo: {Content}", request.Content);

    if (request.Content.Length > 140)
    {
        logger.LogWarning("Rejected todo, content over 140 characters: {Content}", request.Content);
        return Results.BadRequest("Todo content must be at most 140 characters long");
    }

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO todos (content) VALUES ($1) RETURNING id";
    cmd.Parameters.AddWithValue(request.Content);
    int newId = (int)(await cmd.ExecuteScalarAsync())!;
    await PublishTodoStatus("created", newId, request.Content, false);
    return Results.Created();
});

app.MapPut("/todos/{id}", async (int id, DoneRequest request) =>
{
    if (!isHealthy)
    {
        return Results.StatusCode(500);
    }

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE todos SET done = $1 WHERE id = $2";
    cmd.Parameters.AddWithValue(request.Done);
    cmd.Parameters.AddWithValue(id);
    int rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0)
    {
        return Results.NotFound();
    }

    await PublishTodoStatus("updated", id, null, request.Done);
    return Results.Ok();
});

app.Run();

record TodoRequest(string Content);
record TodoDto(int Id, string Content, bool Done);
record DoneRequest(bool Done);
record TodoStatusMessage(string Event, int Id, string? Content, bool? Done);
