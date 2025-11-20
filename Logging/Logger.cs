namespace SoundWatcher.Logging;

/// <summary>
/// Simple file logger that writes to both console and file
/// </summary>
public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SoundWatcher",
        "soundwatcher.log");

    private static readonly object _lock = new();
    private static bool _initialized = false;

    public static void Initialize()
    {
        if (_initialized) return;

        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Clear old log on startup
            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }

            Log("=== SoundWatcher Started ===");
            Log($"Log file: {LogPath}");
            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize logger: {ex.Message}");
        }
    }

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        // Write to console
        Console.WriteLine(logMessage);

        // Write to file
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogPath, logMessage + Environment.NewLine);
            }
        }
        catch
        {
            // Ignore file write errors
        }
    }
}
