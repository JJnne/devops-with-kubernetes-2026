string randomString = Guid.NewGuid().ToString();

while (true)
{
    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
    Console.WriteLine($"{timestamp}: {randomString}");
    Thread.Sleep(TimeSpan.FromSeconds(5));
}
