using System.Net.Http.Json;
using System.Text.Json;
using NATS.Client.Core;

string natsUrl = Environment.GetEnvironmentVariable("NATS_URL")!;
string subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "todo_status";
string? webhookUrl = Environment.GetEnvironmentVariable("GENERIC_WEBHOOK_URL");

HttpClient httpClient = new();
await using var nats = new NatsConnection(new NatsOpts { Url = natsUrl });

Console.WriteLine($"Broadcaster subscribing to '{subject}' on {natsUrl}");

await foreach (var msg in nats.SubscribeAsync<string>(subject, queueGroup: "broadcaster"))
{
    try
    {
        var todoEvent = JsonSerializer.Deserialize<TodoStatusMessage>(msg.Data!);
        if (todoEvent == null)
        {
            continue;
        }

        string message = todoEvent.Event switch
        {
            "created" => $"Todo created: {todoEvent.Content}",
            "updated" => $"Todo {todoEvent.Id} marked {(todoEvent.Done == true ? "done" : "not done")}",
            _ => $"Todo {todoEvent.Id} changed"
        };

        if (string.IsNullOrEmpty(webhookUrl))
        {
            Console.WriteLine($"Logged: {message}");
        }
        else
        {
            await httpClient.PostAsJsonAsync(webhookUrl, new { user = "bot", message });
            Console.WriteLine($"Forwarded: {message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to process message: {ex.Message}");
    }
}

record TodoStatusMessage(string Event, int Id, string? Content, bool? Done);
