string randomString = Guid.NewGuid().ToString();
string filePath = "/usr/src/app/files/log.txt";

while (true)
{
    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
    File.AppendAllText(filePath, $"{timestamp}: {randomString}{Environment.NewLine}");
    Thread.Sleep(TimeSpan.FromSeconds(5));
}
