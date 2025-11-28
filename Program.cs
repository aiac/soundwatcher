using SoundWatcher.Audio;
using SoundWatcher.Configuration;
using SoundWatcher.Network;
using SoundWatcher.UI;
using SoundWatcher.HomeAssistant;
using SoundWatcher.Logging;

namespace SoundWatcher;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Initialize logger first
        Logger.Initialize();
        Logger.Log("Application starting...");

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly AudioMonitor _audioMonitor;
    private readonly AppSettings _settings;
    private HomeAssistantMqtt? _homeAssistant;
    private SettingsForm? _settingsForm;
    private System.Timers.Timer? _offDelayTimer;
    private bool _isMonitoringActive = true;
    private ToolStripMenuItem? _pauseMenuItem;

    public TrayApplicationContext()
    {
        // Load settings
        _settings = AppSettings.Load();

        // Initialize audio monitor
        _audioMonitor = new AudioMonitor();
        _audioMonitor.AudioStateChanged += OnAudioStateChanged;
        _audioMonitor.AudioVolumeChanged += OnAudioVolumeChanged;

        // Initialize Home Assistant MQTT if enabled
        if (_settings.MqttEnabled)
        {
            InitializeHomeAssistant();
        }

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = CreateIconWithOverlay(false),
            Visible = true,
            Text = "SoundWatcher - Monitoring Active"
        };

        // Create context menu
        var contextMenu = new ContextMenuStrip();

        contextMenu.Items.Add("Turn ON", null, (s, e) => SendOnNotifications());
        contextMenu.Items.Add("Turn OFF", null, (s, e) => SendOffNotifications());
        contextMenu.Items.Add("-");
        _pauseMenuItem = new ToolStripMenuItem("Pause Monitoring", null, ToggleMonitoring);
        contextMenu.Items.Add(_pauseMenuItem);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Settings", null, ShowSettings);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, Exit);

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.Click += OnTrayIconClick;
        _trayIcon.DoubleClick += (s, e) => ShowSettings(s, e);

        // Send startup URL if enabled
        if (_settings.StartupUrlEnabled)
        {
            SendOnNotifications();
        }

        // Start monitoring if enabled and devices are configured
        if (_settings.MonitoringEnabled && _settings.MonitoredDeviceIds.Count > 0)
        {
            Logger.Log("[STARTUP] Starting monitoring on application startup...");
            StartMonitoring();
        }
        else
        {
            Logger.Log($"[STARTUP] Monitoring not started: MonitoringEnabled={_settings.MonitoringEnabled}, DeviceCount={_settings.MonitoredDeviceIds.Count}");
        }
    }

    private void StartMonitoring()
    {
        try
        {
            Logger.Log("[START] Getting available devices...");
            var devices = _audioMonitor.GetAllAvailableDevices()
                .Where(d => _settings.MonitoredDeviceIds.Contains(d.Id))
                .ToList();

            Logger.Log($"[START] Found {devices.Count} devices to monitor");

            if (devices.Count == 0)
            {
                _trayIcon.Text = "SoundWatcher - No devices configured";
                Logger.Log("[START] No devices configured, aborting");
                return;
            }

            Logger.Log($"[START] Calling AudioMonitor.StartMonitoring with interval={_settings.CheckIntervalMs}ms, volumeInterval={_settings.VolumeUpdateIntervalMs}ms, threshold={_settings.AudioThreshold}%");
            _audioMonitor.StartMonitoring(devices, _settings.CheckIntervalMs, _settings.VolumeUpdateIntervalMs, _settings.AudioThreshold);
            _isMonitoringActive = true;
            Logger.Log("[START] Monitoring started successfully, updating tray icon...");
            UpdateTrayIcon();
            Logger.Log("[START] StartMonitoring completed");
        }
        catch (Exception ex)
        {
            Logger.Log($"[START] ERROR: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Failed to start monitoring: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopMonitoring()
    {
        _audioMonitor.StopMonitoring();
        _offDelayTimer?.Stop();
        _isMonitoringActive = false;
        UpdateTrayIcon();
    }

    private void ToggleMonitoring(object? sender, EventArgs e)
    {
        if (_isMonitoringActive)
        {
            StopMonitoring();
            if (_pauseMenuItem != null)
                _pauseMenuItem.Text = "Resume Monitoring";
        }
        else
        {
            StartMonitoring();
            if (_pauseMenuItem != null)
                _pauseMenuItem.Text = "Pause Monitoring";
        }
    }

    private async void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        if (e.IsPlaying)
        {
            // Audio started - cancel any pending OFF timer and send ON
            _offDelayTimer?.Stop();
            SendOnNotifications();

            // Update Home Assistant state to "playing"
            if (_homeAssistant != null)
            {
                await _homeAssistant.PublishStateAsync("playing");
            }
        }
        else
        {
            // Audio stopped - wait for delay before sending OFF
            _offDelayTimer?.Stop();
            _offDelayTimer = new System.Timers.Timer(_settings.TurnOffDelayMs);
            _offDelayTimer.Elapsed += async (s, args) =>
            {
                _offDelayTimer.Stop();
                // Check if audio is still not playing before sending OFF
                if (!_audioMonitor.IsAudioPlaying())
                {
                    SendOffNotifications();

                    // Update Home Assistant state to "idle"
                    if (_homeAssistant != null)
                    {
                        await _homeAssistant.PublishStateAsync("idle");
                    }
                }
            };
            _offDelayTimer.AutoReset = false;
            _offDelayTimer.Start();
        }
    }

    private async void InitializeHomeAssistant()
    {
        if (!_settings.MqttEnabled)
        {
            Logger.Log("[HA] MQTT integration is disabled");
            return;
        }

        try
        {
            _homeAssistant = new HomeAssistantMqtt(_settings.MqttDeviceName);
            await _homeAssistant.ConnectAsync(
                _settings.MqttBroker,
                _settings.MqttPort,
                _settings.MqttUsername,
                _settings.MqttPassword
            );
            Logger.Log("[HA] Home Assistant MQTT initialized");
        }
        catch (Exception ex)
        {
            Logger.Log($"[HA] Failed to initialize Home Assistant: {ex.Message}");
            _homeAssistant = null;
        }
    }

    private async void SendOnNotifications()
    {
        if (!_settings.OnUrlsEnabled || _settings.OnUrls.Count == 0)
            return;

        try
        {
            await HttpNotifier.SendOnNotifications(_settings.OnUrls);
        }
        catch (Exception ex)
        {
            // Silently log error - don't disturb user for network issues
            Logger.Log($"Failed to send ON notifications: {ex.Message}");
        }
    }

    private async void SendOffNotifications()
    {
        if (!_settings.OffUrlsEnabled || _settings.OffUrls.Count == 0)
            return;

        try
        {
            await HttpNotifier.SendOffNotifications(_settings.OffUrls);
        }
        catch (Exception ex)
        {
            // Silently log error - don't disturb user for network issues
            Logger.Log($"Failed to send OFF notifications: {ex.Message}");
        }
    }

    private async void OnAudioVolumeChanged(object? sender, AudioVolumeEventArgs e)
    {
        Logger.Log($"[DEBUG] OnAudioVolumeChanged called: level={e.Level}, VolumeUrl={_settings.VolumeUrl}, Enabled={_settings.VolumeUrlEnabled}");

        if (!_settings.VolumeUrlEnabled || string.IsNullOrWhiteSpace(_settings.VolumeUrl))
        {
            Logger.Log($"[DEBUG] VolumeUrl disabled or empty, skipping");
            return;
        }

        try
        {
            await HttpNotifier.SendVolumeNotification(_settings.VolumeUrl, e.Level);
        }
        catch (Exception ex)
        {
            // Silently log error - don't disturb user for network issues
            Logger.Log($"Failed to send volume notification: {ex.Message}");
        }
    }

    private void ShowSettings(object? sender, EventArgs e)
    {
        // Don't open multiple settings windows
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_settings, _audioMonitor);

        if (_settingsForm.ShowDialog() == DialogResult.OK)
        {
            // Settings were saved, restart monitoring with new settings
            StopMonitoring();

            if (_settings.MonitoredDeviceIds.Count > 0)
            {
                StartMonitoring();
            }
        }

        _settingsForm.Dispose();
        _settingsForm = null;
    }

    private void OnTrayIconClick(object? sender, EventArgs e)
    {
        // Only handle left click (not right click which opens context menu)
        if (e is MouseEventArgs mouseEvent && mouseEvent.Button == MouseButtons.Left)
        {
            ToggleMonitoring(sender, e);
        }
    }

    private void UpdateTrayIcon()
    {
        if (_isMonitoringActive)
        {
            _trayIcon.Text = "SoundWatcher - Monitoring Active";
            _trayIcon.Icon = CreateIconWithOverlay(false);
        }
        else
        {
            _trayIcon.Text = "SoundWatcher - Paused";
            _trayIcon.Icon = CreateIconWithOverlay(true);
        }
    }

    private Icon CreateIconWithOverlay(bool paused)
    {
        var baseIcon = LoadIcon();

        if (!paused)
            return baseIcon;

        // Create a bitmap from the icon with a white pause overlay
        try
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                // Draw base icon
                g.DrawIcon(baseIcon, new Rectangle(0, 0, 32, 32));

                // Draw white pause symbol overlay (two vertical bars)
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, 10, 20, 4, 10);
                    g.FillRectangle(brush, 18, 20, 4, 10);
                }
            }

            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
        }
        catch
        {
            return baseIcon;
        }
    }

    private void Exit(object? sender, EventArgs e)
    {
        // Send exit URL if enabled
        if (_settings.ExitUrlEnabled)
        {
            SendOffNotifications();
            // Give a brief moment for the request to complete
            System.Threading.Thread.Sleep(500);
        }

        _trayIcon.Visible = false;
        _audioMonitor.Dispose();
        _offDelayTimer?.Dispose();
        _homeAssistant?.Dispose();
        Application.Exit();
    }

    private Icon LoadIcon()
    {
        try
        {
            // Load icon from embedded resources
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "SoundWatcher.Resources.AppIcon.ico";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
        }
        catch { }

        // Fallback to system icon
        return SystemIcons.Application;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon?.Dispose();
            _audioMonitor?.Dispose();
            _offDelayTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
