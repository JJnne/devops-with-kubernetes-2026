using Npgsql;

string host = Environment.GetEnvironmentVariable("POSTGRES_HOST")!;
string port = Environment.GetEnvironmentVariable("POSTGRES_PORT")!;
string database = Environment.GetEnvironmentVariable("POSTGRES_DB")!;
string username = Environment.GetEnvironmentVariable("POSTGRES_USER")!;
string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")!;
string connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

await using (var initConn = new NpgsqlConnection(connectionString))
{
    await initConn.OpenAsync();
    await using var createCmd = initConn.CreateCommand();
    createCmd.CommandText = "CREATE TABLE IF NOT EXISTS counter (id INT PRIMARY KEY, value INT NOT NULL)";
    await createCmd.ExecuteNonQueryAsync();

    await using var seedCmd = initConn.CreateCommand();
    seedCmd.CommandText = "INSERT INTO counter (id, value) VALUES (1, 0) ON CONFLICT (id) DO NOTHING";
    await seedCmd.ExecuteNonQueryAsync();
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3000");
var app = builder.Build();

app.MapGet("/pingpong", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE counter SET value = value + 1 WHERE id = 1 RETURNING value - 1";
    int previous = (int)(await cmd.ExecuteScalarAsync())!;
    return $"pong {previous}";
});

app.MapGet("/pingpong/count", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT value FROM counter WHERE id = 1";
    int value = (int)(await cmd.ExecuteScalarAsync())!;
    return value.ToString();
});

app.Run();
