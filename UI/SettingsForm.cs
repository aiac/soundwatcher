using SoundWatcher.Audio;
using SoundWatcher.Configuration;

namespace SoundWatcher.UI;

public partial class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly AudioMonitor _audioMonitor;

    // UI Controls
    private TabControl tabControl = null!;
    private TabPage devicesTab = null!;
    private TabPage urlsTab = null!;
    private TabPage timingsTab = null!;
    private TabPage homeAssistantTab = null!;
    private TabPage wledTab = null!;

    // Device controls
    private CheckedListBox deviceListBox = null!;
    private Button refreshDevicesButton = null!;
    private Label deviceStatusLabel = null!;

    // URL controls
    private CheckBox onUrlsEnabledCheckBox = null!;
    private TextBox onUrl1TextBox = null!;
    private TextBox onUrl2TextBox = null!;
    private CheckBox offUrlsEnabledCheckBox = null!;
    private TextBox offUrl1TextBox = null!;
    private TextBox offUrl2TextBox = null!;
    private CheckBox volumeUrlEnabledCheckBox = null!;
    private TextBox volumeUrlTextBox = null!;
    private CheckBox startupUrlEnabledCheckBox = null!;
    private CheckBox exitUrlEnabledCheckBox = null!;
    private Button testOnButton = null!;
    private Button testOffButton = null!;

    // Timing controls
    private NumericUpDown checkIntervalNumeric = null!;
    private NumericUpDown offDelayNumeric = null!;
    private NumericUpDown volumeIntervalNumeric = null!;
    private NumericUpDown audioThresholdNumeric = null!;

    // Home Assistant controls
    private CheckBox mqttEnabledCheckBox = null!;
    private TextBox mqttBrokerTextBox = null!;
    private NumericUpDown mqttPortNumeric = null!;
    private TextBox mqttUsernameTextBox = null!;
    private TextBox mqttPasswordTextBox = null!;
    private TextBox mqttDeviceNameTextBox = null!;

    // WLED controls
    private CheckBox wledEnabledCheckBox = null!;
    private TextBox wledHostTextBox = null!;
    private NumericUpDown wledPortNumeric = null!;
    private NumericUpDown wledLedCountNumeric = null!;
    private NumericUpDown wledUpdateIntervalNumeric = null!;
    private ComboBox wledVisualizationModeCombo = null!;
    private TextBox wledColorTextBox = null!;
    private NumericUpDown wledPeakDecayNumeric = null!;

    // Bottom buttons
    private Button saveButton = null!;
    private Button cancelButton = null!;

    public SettingsForm(AppSettings settings, AudioMonitor audioMonitor)
    {
        _settings = settings;
        _audioMonitor = audioMonitor;
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text = "SoundWatcher Settings";
        Size = new Size(600, 550);  // Increased from 500 to 550
        MinimumSize = new Size(500, 450);  // Increased from 400 to 450
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        // Tab control
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 5)
        };

        // Devices tab
        devicesTab = new TabPage("Audio Devices");
        InitializeDevicesTab();
        tabControl.TabPages.Add(devicesTab);

        // URLs tab
        urlsTab = new TabPage("HTTP Notifications");
        InitializeUrlsTab();
        tabControl.TabPages.Add(urlsTab);

        // Timings tab
        timingsTab = new TabPage("Timing Settings");
        InitializeTimingsTab();
        tabControl.TabPages.Add(timingsTab);

        // Home Assistant tab
        homeAssistantTab = new TabPage("Home Assistant");
        InitializeHomeAssistantTab();
        tabControl.TabPages.Add(homeAssistantTab);

        // WLED tab
        wledTab = new TabPage("WLED");
        InitializeWledTab();
        tabControl.TabPages.Add(wledTab);

        // Bottom panel with buttons
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            Padding = new Padding(10)
        };

        saveButton = new Button
        {
            Text = "Save",
            Width = 80,
            Height = 30,
            Anchor = AnchorStyles.Right,
            DialogResult = DialogResult.OK
        };
        saveButton.Location = new Point(bottomPanel.Width - 180, 10);
        saveButton.Click += SaveButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Width = 80,
            Height = 30,
            Anchor = AnchorStyles.Right,
            DialogResult = DialogResult.Cancel
        };
        cancelButton.Location = new Point(bottomPanel.Width - 90, 10);

        bottomPanel.Controls.AddRange(new Control[] { saveButton, cancelButton });

        Controls.Add(tabControl);
        Controls.Add(bottomPanel);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void InitializeDevicesTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        var label = new Label
        {
            Text = "Select audio devices to monitor:",
            AutoSize = true,
            Location = new Point(0, 0)
        };

        deviceListBox = new CheckedListBox
        {
            Location = new Point(0, 30),
            Size = new Size(540, 250),
            CheckOnClick = true
        };

        refreshDevicesButton = new Button
        {
            Text = "Refresh Devices",
            Location = new Point(0, 290),
            Size = new Size(120, 30)
        };
        refreshDevicesButton.Click += RefreshDevicesButton_Click;

        deviceStatusLabel = new Label
        {
            Text = "",
            Location = new Point(130, 295),
            AutoSize = true,
            ForeColor = Color.Gray
        };

        var noteLabel = new Label
        {
            Text = "ℹ Both Output and Input devices are supported.\n" +
                   "Input devices use silent capture to enable monitoring.",
            Location = new Point(0, 330),
            AutoSize = false,
            Size = new Size(540, 40),
            ForeColor = Color.Blue,
            Font = new Font(Font, FontStyle.Regular)
        };

        panel.Controls.AddRange(new Control[] {
            label, deviceListBox, refreshDevicesButton, deviceStatusLabel, noteLabel
        });
        devicesTab.Controls.Add(panel);
    }

    private void InitializeUrlsTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        int y = 0;

        // ON URLs
        onUrlsEnabledCheckBox = new CheckBox
        {
            Text = "Enable ON URLs (when audio starts)",
            Location = new Point(0, y),
            AutoSize = true,
            Checked = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        y += 22;  // Reduced from 30

        var onUrl1Label = new Label { Text = "URL 1:", Location = new Point(20, y), Width = 50 };
        onUrl1TextBox = new TextBox { Location = new Point(80, y - 3), Width = 460 };
        y += 28;  // Reduced from 35

        var onUrl2Label = new Label { Text = "URL 2:", Location = new Point(20, y), Width = 50 };
        onUrl2TextBox = new TextBox { Location = new Point(80, y - 3), Width = 460 };
        y += 28;  // Reduced from 35

        testOnButton = new Button
        {
            Text = "Test ON URLs",
            Location = new Point(20, y),
            Size = new Size(120, 25)
        };
        testOnButton.Click += TestOnButton_Click;
        y += 35;  // Reduced from 45

        // OFF URLs
        offUrlsEnabledCheckBox = new CheckBox
        {
            Text = "Enable OFF URLs (when audio stops)",
            Location = new Point(0, y),
            AutoSize = true,
            Checked = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        y += 22;  // Reduced from 30

        var offUrl1Label = new Label { Text = "URL 1:", Location = new Point(20, y), Width = 50 };
        offUrl1TextBox = new TextBox { Location = new Point(80, y - 3), Width = 460 };
        y += 28;  // Reduced from 35

        var offUrl2Label = new Label { Text = "URL 2:", Location = new Point(20, y), Width = 50 };
        offUrl2TextBox = new TextBox { Location = new Point(80, y - 3), Width = 460 };
        y += 28;  // Reduced from 35

        testOffButton = new Button
        {
            Text = "Test OFF URLs",
            Location = new Point(20, y),
            Size = new Size(120, 25)
        };
        testOffButton.Click += TestOffButton_Click;
        y += 35;  // Reduced from 45

        // Peak Level URL
        volumeUrlEnabledCheckBox = new CheckBox
        {
            Text = "Enable Peak Level URL (real-time LED control)",
            Location = new Point(0, y),
            AutoSize = true,
            Checked = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        y += 22;  // Reduced from 30

        var volumeUrlLabel = new Label { Text = "URL:", Location = new Point(20, y), Width = 50 };
        volumeUrlTextBox = new TextBox { Location = new Point(80, y - 3), Width = 460 };
        y += 28;  // Reduced from 35

        var volumeHelpLabel = new Label
        {
            Text = "Use {VOL} in URL - replaced with peak (0-100) for LED strips. Example:\nhttp://192.168.1.219/?m=1&d0={VOL}",
            Location = new Point(80, y),
            AutoSize = false,
            Size = new Size(460, 35),
            ForeColor = Color.Gray
        };
        y += 45;

        // Startup/Exit URLs
        startupUrlEnabledCheckBox = new CheckBox
        {
            Text = "Send ON URLs on app startup",
            Location = new Point(0, y),
            AutoSize = true
        };
        y += 25;

        exitUrlEnabledCheckBox = new CheckBox
        {
            Text = "Send OFF URLs on app exit",
            Location = new Point(0, y),
            AutoSize = true
        };

        panel.Controls.AddRange(new Control[]
        {
            onUrlsEnabledCheckBox, onUrl1Label, onUrl1TextBox, onUrl2Label, onUrl2TextBox, testOnButton,
            offUrlsEnabledCheckBox, offUrl1Label, offUrl1TextBox, offUrl2Label, offUrl2TextBox, testOffButton,
            volumeUrlEnabledCheckBox, volumeUrlLabel, volumeUrlTextBox, volumeHelpLabel,
            startupUrlEnabledCheckBox, exitUrlEnabledCheckBox
        });

        urlsTab.Controls.Add(panel);
    }

    private void InitializeTimingsTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        int y = 0;

        var checkLabel = new Label
        {
            Text = "Check interval (milliseconds):",
            Location = new Point(0, y),
            AutoSize = true
        };
        y += 20;  // Reduced from 25

        checkIntervalNumeric = new NumericUpDown
        {
            Location = new Point(0, y),
            Width = 150,
            Minimum = 100,
            Maximum = 100000,
            Increment = 100,
            Value = 1000
        };
        y += 28;  // Reduced from 35

        var checkHelp = new Label
        {
            Text = "How often to check for audio activity (lower = more responsive, higher = less CPU usage)",
            Location = new Point(0, y),
            AutoSize = false,
            Size = new Size(540, 30),
            ForeColor = Color.Gray
        };
        y += 35;  // Reduced from 45

        var offDelayLabel = new Label
        {
            Text = "Turn OFF delay (milliseconds):",
            Location = new Point(0, y),
            AutoSize = true
        };
        y += 20;  // Reduced from 25

        offDelayNumeric = new NumericUpDown
        {
            Location = new Point(0, y),
            Width = 150,
            Minimum = 1000,
            Maximum = 1000000,
            Increment = 1000,
            Value = 30000
        };
        y += 28;  // Reduced from 35

        var offHelp = new Label
        {
            Text = "Additional delay before sending OFF notifications (useful to avoid flickering)",
            Location = new Point(0, y),
            AutoSize = false,
            Size = new Size(540, 30),
            ForeColor = Color.Gray
        };
        y += 35;  // Reduced from 45

        var volumeIntervalLabel = new Label
        {
            Text = "HTTP peak update interval (milliseconds, 0 = disabled):",
            Location = new Point(0, y),
            AutoSize = true
        };
        y += 20;  // Reduced from 25

        volumeIntervalNumeric = new NumericUpDown
        {
            Location = new Point(0, y),
            Width = 150,
            Minimum = 0,
            Maximum = 60000,
            Increment = 10,
            Value = 0
        };
        y += 28;  // Reduced from 35

        var volumeHelp = new Label
        {
            Text = "How often to send HTTP peak updates (0 to disable, typically 10-100ms). For WLED, use the WLED update interval setting instead.",
            Location = new Point(0, y),
            AutoSize = false,
            Size = new Size(540, 30),
            ForeColor = Color.Gray
        };
        y += 35;  // Reduced from 45

        var thresholdLabel = new Label
        {
            Text = "Audio detection threshold (%):",
            Location = new Point(0, y),
            AutoSize = true
        };
        y += 20;  // Reduced from 25

        audioThresholdNumeric = new NumericUpDown
        {
            Location = new Point(0, y),
            Width = 150,
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 2.0m
        };
        y += 35;

        var thresholdHelp = new Label
        {
            Text = "Minimum audio level (0-100%) to detect as active sound. Higher values reduce false triggers from background noise.",
            Location = new Point(0, y),
            AutoSize = false,
            Size = new Size(540, 30),
            ForeColor = Color.Gray
        };

        panel.Controls.AddRange(new Control[]
        {
            checkLabel, checkIntervalNumeric, checkHelp,
            offDelayLabel, offDelayNumeric, offHelp,
            volumeIntervalLabel, volumeIntervalNumeric, volumeHelp,
            thresholdLabel, audioThresholdNumeric, thresholdHelp
        });

        timingsTab.Controls.Add(panel);
    }

    private void LoadSettings()
    {
        // Load URLs
        onUrlsEnabledCheckBox.Checked = _settings.OnUrlsEnabled;
        if (_settings.OnUrls.Count > 0) onUrl1TextBox.Text = _settings.OnUrls[0];
        if (_settings.OnUrls.Count > 1) onUrl2TextBox.Text = _settings.OnUrls[1];
        offUrlsEnabledCheckBox.Checked = _settings.OffUrlsEnabled;
        if (_settings.OffUrls.Count > 0) offUrl1TextBox.Text = _settings.OffUrls[0];
        if (_settings.OffUrls.Count > 1) offUrl2TextBox.Text = _settings.OffUrls[1];
        volumeUrlEnabledCheckBox.Checked = _settings.VolumeUrlEnabled;
        volumeUrlTextBox.Text = _settings.VolumeUrl;
        startupUrlEnabledCheckBox.Checked = _settings.StartupUrlEnabled;
        exitUrlEnabledCheckBox.Checked = _settings.ExitUrlEnabled;

        // Load timings
        checkIntervalNumeric.Value = _settings.CheckIntervalMs;
        offDelayNumeric.Value = _settings.TurnOffDelayMs;
        volumeIntervalNumeric.Value = _settings.VolumeUpdateIntervalMs;
        audioThresholdNumeric.Value = (decimal)_settings.AudioThreshold;

        // Load Home Assistant settings
        mqttEnabledCheckBox.Checked = _settings.MqttEnabled;
        mqttBrokerTextBox.Text = _settings.MqttBroker;
        mqttPortNumeric.Value = _settings.MqttPort;
        mqttUsernameTextBox.Text = _settings.MqttUsername;
        mqttPasswordTextBox.Text = _settings.MqttPassword;
        mqttDeviceNameTextBox.Text = _settings.MqttDeviceName;

        // Load WLED settings
        wledEnabledCheckBox.Checked = _settings.WledEnabled;
        wledHostTextBox.Text = _settings.WledHost;
        wledPortNumeric.Value = _settings.WledPort;
        wledLedCountNumeric.Value = _settings.WledLedCount;
        wledUpdateIntervalNumeric.Value = _settings.WledUpdateIntervalMs;
        wledVisualizationModeCombo.SelectedIndex = _settings.WledVisualizationMode;
        wledColorTextBox.Text = _settings.WledColor;
        wledPeakDecayNumeric.Value = _settings.WledPeakDecayMs;

        // Load devices
        LoadDevices();
    }

    private void LoadDevices()
    {
        deviceListBox.Items.Clear();

        try
        {
            var devices = _audioMonitor.GetAllAvailableDevices();

            foreach (var device in devices)
            {
                var index = deviceListBox.Items.Add(new DeviceListItem(device, device.Name));

                // Check if this device was previously selected
                if (_settings.MonitoredDeviceIds.Contains(device.Id))
                {
                    deviceListBox.SetItemChecked(index, true);
                }
            }

            deviceStatusLabel.Text = $"Found {devices.Count} device(s)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading devices: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshDevicesButton_Click(object? sender, EventArgs e)
    {
        LoadDevices();
    }

    private async void TestOnButton_Click(object? sender, EventArgs e)
    {
        testOnButton.Enabled = false;
        try
        {
            var urls = new List<string>();
            if (!string.IsNullOrWhiteSpace(onUrl1TextBox.Text)) urls.Add(onUrl1TextBox.Text);
            if (!string.IsNullOrWhiteSpace(onUrl2TextBox.Text)) urls.Add(onUrl2TextBox.Text);

            if (urls.Count == 0)
            {
                MessageBox.Show("Please enter at least one ON URL", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await Network.HttpNotifier.SendOnNotifications(urls);
            MessageBox.Show("ON notifications sent successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error sending ON notifications: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            testOnButton.Enabled = true;
        }
    }

    private async void TestOffButton_Click(object? sender, EventArgs e)
    {
        testOffButton.Enabled = false;
        try
        {
            var urls = new List<string>();
            if (!string.IsNullOrWhiteSpace(offUrl1TextBox.Text)) urls.Add(offUrl1TextBox.Text);
            if (!string.IsNullOrWhiteSpace(offUrl2TextBox.Text)) urls.Add(offUrl2TextBox.Text);

            if (urls.Count == 0)
            {
                MessageBox.Show("Please enter at least one OFF URL", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await Network.HttpNotifier.SendOffNotifications(urls);
            MessageBox.Show("OFF notifications sent successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error sending OFF notifications: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            testOffButton.Enabled = true;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // Save URLs
        _settings.OnUrlsEnabled = onUrlsEnabledCheckBox.Checked;
        _settings.OnUrls.Clear();
        if (!string.IsNullOrWhiteSpace(onUrl1TextBox.Text)) _settings.OnUrls.Add(onUrl1TextBox.Text);
        if (!string.IsNullOrWhiteSpace(onUrl2TextBox.Text)) _settings.OnUrls.Add(onUrl2TextBox.Text);

        _settings.OffUrlsEnabled = offUrlsEnabledCheckBox.Checked;
        _settings.OffUrls.Clear();
        if (!string.IsNullOrWhiteSpace(offUrl1TextBox.Text)) _settings.OffUrls.Add(offUrl1TextBox.Text);
        if (!string.IsNullOrWhiteSpace(offUrl2TextBox.Text)) _settings.OffUrls.Add(offUrl2TextBox.Text);

        _settings.VolumeUrlEnabled = volumeUrlEnabledCheckBox.Checked;
        _settings.VolumeUrl = volumeUrlTextBox.Text;
        _settings.StartupUrlEnabled = startupUrlEnabledCheckBox.Checked;
        _settings.ExitUrlEnabled = exitUrlEnabledCheckBox.Checked;

        // Save timings
        _settings.CheckIntervalMs = (int)checkIntervalNumeric.Value;
        _settings.TurnOffDelayMs = (int)offDelayNumeric.Value;
        _settings.VolumeUpdateIntervalMs = (int)volumeIntervalNumeric.Value;
        _settings.AudioThreshold = (float)audioThresholdNumeric.Value;

        Console.WriteLine($"[DEBUG] Saving settings: VolumeUrl={_settings.VolumeUrl}, VolumeInterval={_settings.VolumeUpdateIntervalMs}");

        // Save selected devices
        _settings.MonitoredDeviceIds.Clear();
        foreach (var item in deviceListBox.CheckedItems)
        {
            if (item is DeviceListItem deviceItem)
            {
                _settings.MonitoredDeviceIds.Add(deviceItem.Device.Id);
            }
        }

        // Save Home Assistant settings
        _settings.MqttEnabled = mqttEnabledCheckBox.Checked;
        _settings.MqttBroker = mqttBrokerTextBox.Text;
        _settings.MqttPort = (int)mqttPortNumeric.Value;
        _settings.MqttUsername = mqttUsernameTextBox.Text;
        _settings.MqttPassword = mqttPasswordTextBox.Text;
        _settings.MqttDeviceName = mqttDeviceNameTextBox.Text;

        // Save WLED settings
        _settings.WledEnabled = wledEnabledCheckBox.Checked;
        _settings.WledHost = wledHostTextBox.Text;
        _settings.WledPort = (int)wledPortNumeric.Value;
        _settings.WledLedCount = (int)wledLedCountNumeric.Value;
        _settings.WledUpdateIntervalMs = (int)wledUpdateIntervalNumeric.Value;
        _settings.WledVisualizationMode = wledVisualizationModeCombo.SelectedIndex;
        _settings.WledColor = wledColorTextBox.Text;
        _settings.WledPeakDecayMs = (int)wledPeakDecayNumeric.Value;

        _settings.Save();
    }

    private void InitializeHomeAssistantTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        int y = 0;

        mqttEnabledCheckBox = new CheckBox
        {
            Text = "Enable Home Assistant MQTT Discovery",
            Location = new Point(0, y),
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        y += 28;  // Reduced from 35

        var brokerLabel = new Label
        {
            Text = "MQTT Broker:",
            Location = new Point(0, y),
            Width = 120
        };
        mqttBrokerTextBox = new TextBox
        {
            Location = new Point(130, y - 3),
            Width = 200,
            Text = "localhost"
        };
        y += 28;  // Reduced from 35

        var portLabel = new Label
        {
            Text = "MQTT Port:",
            Location = new Point(0, y),
            Width = 120
        };
        mqttPortNumeric = new NumericUpDown
        {
            Location = new Point(130, y - 3),
            Width = 100,
            Minimum = 1,
            Maximum = 65535,
            Value = 1883
        };
        y += 28;  // Reduced from 35

        var usernameLabel = new Label
        {
            Text = "Username (optional):",
            Location = new Point(0, y),
            Width = 120
        };
        mqttUsernameTextBox = new TextBox
        {
            Location = new Point(130, y - 3),
            Width = 200
        };
        y += 28;  // Reduced from 35

        var passwordLabel = new Label
        {
            Text = "Password (optional):",
            Location = new Point(0, y),
            Width = 120
        };
        mqttPasswordTextBox = new TextBox
        {
            Location = new Point(130, y - 3),
            Width = 200,
            PasswordChar = '●'
        };
        y += 28;  // Reduced from 35

        var deviceNameLabel = new Label
        {
            Text = "Device Name:",
            Location = new Point(0, y),
            Width = 120
        };
        mqttDeviceNameTextBox = new TextBox
        {
            Location = new Point(130, y - 3),
            Width = 300,
            Text = "Windows Audio Monitor"
        };
        y += 40;

        var infoLabel = new Label
        {
            Text = "This will create a media_player entity in Home Assistant with states:\n" +
                   "• playing - Audio is currently playing\n" +
                   "• idle - No audio detected\n" +
                   "• unknown - Monitoring not active\n\n" +
                   "Entity ID: media_player.windows_audio_monitor",
            Location = new Point(0, y),
            AutoSize = false,
            Size = new Size(540, 100),
            ForeColor = Color.Gray
        };

        panel.Controls.AddRange(new Control[]
        {
            mqttEnabledCheckBox,
            brokerLabel, mqttBrokerTextBox,
            portLabel, mqttPortNumeric,
            usernameLabel, mqttUsernameTextBox,
            passwordLabel, mqttPasswordTextBox,
            deviceNameLabel, mqttDeviceNameTextBox,
            infoLabel
        });

        homeAssistantTab.Controls.Add(panel);
    }

    private void InitializeWledTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        int y = 0;

        wledEnabledCheckBox = new CheckBox
        {
            Text = "Enable WLED UDP Integration",
            Location = new Point(0, y),
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        y += 35;

        var hostLabel = new Label
        {
            Text = "WLED Host (IP or hostname):",
            Location = new Point(0, y),
            Width = 180
        };
        wledHostTextBox = new TextBox
        {
            Location = new Point(190, y - 3),
            Width = 300,
            PlaceholderText = "192.168.1.100 or wled.local"
        };
        y += 35;

        var portLabel = new Label
        {
            Text = "WLED UDP Port:",
            Location = new Point(0, y),
            Width = 180
        };
        wledPortNumeric = new NumericUpDown
        {
            Location = new Point(190, y - 3),
            Width = 100,
            Minimum = 1,
            Maximum = 65535,
            Value = 21324
        };
        y += 35;

        var ledCountLabel = new Label
        {
            Text = "Number of LEDs:",
            Location = new Point(0, y),
            Width = 180
        };
        wledLedCountNumeric = new NumericUpDown
        {
            Location = new Point(190, y - 3),
            Width = 100,
            Minimum = 1,
            Maximum = 490,
            Value = 60
        };
        y += 35;

        var updateIntervalLabel = new Label
        {
            Text = "WLED update interval (ms):",
            Location = new Point(0, y),
            Width = 180
        };
        wledUpdateIntervalNumeric = new NumericUpDown
        {
            Location = new Point(190, y - 3),
            Width = 100,
            Minimum = 0,
            Maximum = 60000,
            Increment = 10,
            Value = 0
        };
        y += 28;

        var updateHelp = new Label
        {
            Text = "0 = disabled, typically 10-50ms for smooth LED animations",
            Location = new Point(190, y),
            AutoSize = false,
            Size = new Size(350, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        var visualizationLabel = new Label
        {
            Text = "Visualization mode:",
            Location = new Point(0, y),
            Width = 180
        };
        wledVisualizationModeCombo = new ComboBox
        {
            Location = new Point(190, y - 3),
            Width = 300,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        wledVisualizationModeCombo.Items.AddRange(new object[]
        {
            "Gradient (Blue→Cyan→Green→Yellow→Red)",
            "Left to Right (more peak = more LEDs)",
            "Center Out (more peak = more LEDs from center)",
            "Brightness (all LEDs dim/bright by peak)"
        });
        wledVisualizationModeCombo.SelectedIndex = 0;
        y += 35;

        var colorLabel = new Label
        {
            Text = "LED Color (hex):",
            Location = new Point(0, y),
            Width = 180
        };
        wledColorTextBox = new TextBox
        {
            Location = new Point(190, y - 3),
            Width = 100,
            Text = "#FF0000",
            PlaceholderText = "#RRGGBB"
        };
        var colorHelp = new Label
        {
            Text = "Only used for modes 2-4 (e.g. #FF0000 for red, #00FF00 for green)",
            Location = new Point(300, y),
            AutoSize = false,
            Size = new Size(240, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        var decayLabel = new Label
        {
            Text = "VU meter decay (ms):",
            Location = new Point(0, y),
            Width = 180
        };
        wledPeakDecayNumeric = new NumericUpDown
        {
            Location = new Point(190, y - 3),
            Width = 100,
            Minimum = 50,
            Maximum = 500,
            Increment = 10,
            Value = 150
        };
        var decayHelp = new Label
        {
            Text = "How fast the meter falls (50-500ms, typical 100-200ms)",
            Location = new Point(300, y),
            AutoSize = false,
            Size = new Size(240, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        var infoLabel = new Label
        {
            Text = "WLED UDP Realtime Integration\n\n" +
                   "This sends audio peak data to WLED devices via UDP for real-time LED visualization.\n\n" +
                   "Requirements:\n" +
                   "• WLED device on your local network\n" +
                   "• UDP Realtime enabled in WLED settings (default port: 21324)",
            Location = new Point(0, y),
            AutoSize = false,
            Size = new Size(540, 120),
            ForeColor = Color.Gray
        };

        panel.Controls.AddRange(new Control[]
        {
            wledEnabledCheckBox,
            hostLabel, wledHostTextBox,
            portLabel, wledPortNumeric,
            ledCountLabel, wledLedCountNumeric,
            updateIntervalLabel, wledUpdateIntervalNumeric, updateHelp,
            visualizationLabel, wledVisualizationModeCombo,
            colorLabel, wledColorTextBox, colorHelp,
            decayLabel, wledPeakDecayNumeric, decayHelp,
            infoLabel
        });

        wledTab.Controls.Add(panel);
    }

    private class DeviceListItem
    {
        public AudioDeviceInfo Device { get; }
        private readonly string _displayName;

        public DeviceListItem(AudioDeviceInfo device, string displayName)
        {
            Device = device;
            _displayName = displayName;
        }

        public override string ToString() => _displayName;
    }
}
