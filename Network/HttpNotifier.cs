using SoundWatcher.Logging;
namespace SoundWatcher.Network;

/// <summary>
/// Sends HTTP notifications when audio state changes
/// </summary>
public class HttpNotifier
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// Sends ON notifications to all configured URLs
    /// </summary>
    public static async Task SendOnNotifications(List<string> urls)
    {
        await SendNotifications(urls, "ON");
    }

    /// <summary>
    /// Sends OFF notifications to all configured URLs
    /// </summary>
    public static async Task SendOffNotifications(List<string> urls)
    {
        await SendNotifications(urls, "OFF");
    }

    /// <summary>
    /// Sends peak level notification for LED control, replacing {VOL} with peak (0-100)
    /// </summary>
    public static async Task SendVolumeNotification(string urlTemplate, int level)
    {
        if (string.IsNullOrWhiteSpace(urlTemplate))
            return;

        // Replace {VOL} placeholder with actual peak level
        var url = urlTemplate.Replace("{VOL}", level.ToString());

        Logger.Log($"[DEBUG] Sending peak notification: level={level}, url={url}");

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            Logger.Log($"[DEBUG] Peak notification sent successfully: {url}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to send peak notification to {url}: {ex.Message}");
        }
    }

    private static async Task SendNotifications(List<string> urls, string type)
    {
        if (urls == null || urls.Count == 0)
        {
            Logger.Log($"[DEBUG] No {type} URLs configured, skipping");
            return;
        }

        var tasks = new List<Task>();

        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url))
                continue;

            tasks.Add(SendSingleNotification(url, type));
        }

        if (tasks.Count == 0)
        {
            Logger.Log($"[DEBUG] All {type} URLs are blank, skipping");
            return;
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Log error but don't show to user for every failure
            Logger.Log($"Error sending {type} notifications: {ex.Message}");
        }
    }

    private static async Task SendSingleNotification(string url, string type)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to send {type} notification to {url}: {ex.Message}");
            throw; // Re-throw to be caught by Task.WhenAll
        }
    }
}
