using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoundWatcher.Configuration;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    public List<string> OnUrls { get; set; } = new();
    public List<string> OffUrls { get; set; } = new();
    public string VolumeUrl { get; set; } = string.Empty;
    public int VolumeUpdateIntervalMs { get; set; } = 0; // 0 = disabled

    // Enable/disable individual URLs
    public bool OnUrlsEnabled { get; set; } = true;
    public bool OffUrlsEnabled { get; set; } = true;
    public bool VolumeUrlEnabled { get; set; } = true;
    public int CheckIntervalMs { get; set; } = 1000;
    public int TurnOffDelayMs { get; set; } = 30000;
    public List<string> MonitoredDeviceIds { get; set; } = new();
    public bool MonitoringEnabled { get; set; } = true;
    public float AudioThreshold { get; set; } = 2.0f; // Percentage (0-100), default 2%

    // Home Assistant MQTT settings
    public bool MqttEnabled { get; set; } = false;
    public string MqttBroker { get; set; } = "localhost";
    public int MqttPort { get; set; } = 1883;
    public string MqttUsername { get; set; } = string.Empty;
    public string MqttPassword { get; set; } = string.Empty;
    public string MqttDeviceName { get; set; } = "Windows Audio Monitor";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SoundWatcher",
        "settings.json");

    /// <summary>
    /// Loads settings from disk
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return default settings
        }

        return new AppSettings();
    }

    /// <summary>
    /// Saves settings to disk
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
