using SoundWatcher.Logging;
namespace SoundWatcher.Audio;

/// <summary>
/// Audio monitor for WASAPI devices
/// </summary>
public class AudioMonitor : IDisposable
{
    private readonly WasapiMonitor _wasapiMonitor;
    private System.Timers.Timer? _checkTimer;
    private System.Timers.Timer? _volumeTimer;
    private System.Timers.Timer? _wledTimer;
    private System.Timers.Timer? _peakSamplingTimer;
    private bool _isMonitoring;
    private bool _lastState;
    private bool _disposed;

    // Peak smoothing - collect samples and send average
    private readonly List<int> _peakSamples = new();
    private readonly object _peakLock = new();
    private const int PEAK_SAMPLE_RATE_MS = 10; // Sample every 10ms for smoothing

    // Dynamic interval optimization
    private int _baseCheckIntervalMs;
    private int _volumeUpdateIntervalMs;
    private int _wledUpdateIntervalMs;

    public event EventHandler<AudioStateChangedEventArgs>? AudioStateChanged;
    public event EventHandler<AudioVolumeEventArgs>? AudioVolumeChanged;
    public event EventHandler<AudioVolumeEventArgs>? WledVolumeChanged;

    public AudioMonitor()
    {
        _wasapiMonitor = new WasapiMonitor();
    }

    /// <summary>
    /// Gets all available WASAPI audio devices
    /// </summary>
    public List<AudioDeviceInfo> GetAllAvailableDevices()
    {
        return _wasapiMonitor.GetAvailableDevices();
    }

    /// <summary>
    /// Starts monitoring specified devices
    /// </summary>
    /// <param name="devices">List of device IDs to monitor</param>
    /// <param name="checkIntervalMs">Base interval in milliseconds to check for audio when OFF or not sending peak data</param>
    /// <param name="volumeUpdateIntervalMs">Interval in milliseconds to send HTTP volume updates (0 to disable)</param>
    /// <param name="wledUpdateIntervalMs">Interval in milliseconds to send WLED volume updates (0 to disable)</param>
    /// <param name="audioThresholdPercent">Audio detection threshold as percentage (0-100)</param>
    public void StartMonitoring(List<AudioDeviceInfo> devices, int checkIntervalMs, int volumeUpdateIntervalMs = 0, int wledUpdateIntervalMs = 0, float audioThresholdPercent = 2.0f)
    {
        StopMonitoring();

        if (devices == null || devices.Count == 0)
            return;

        // Store configuration
        _baseCheckIntervalMs = checkIntervalMs;
        _volumeUpdateIntervalMs = volumeUpdateIntervalMs;
        _wledUpdateIntervalMs = wledUpdateIntervalMs;

        // Set audio threshold
        _wasapiMonitor.SetAudioThreshold(audioThresholdPercent);

        // Start WASAPI monitoring
        _wasapiMonitor.StartMonitoring(devices);

        // Optimization: Start with base check interval (audio is OFF initially)
        // When audio is OFF or ON without peak monitoring: use baseCheckIntervalMs (e.g. 1000ms)
        // When audio is ON with peak monitoring: use volumeUpdateIntervalMs (e.g. 120ms)
        _checkTimer = new System.Timers.Timer(checkIntervalMs);
        _checkTimer.Elapsed += CheckAudioState;
        _checkTimer.AutoReset = true;
        _checkTimer.Start();

        Logger.Log($"[OPTIMIZE] Starting with check interval: {checkIntervalMs}ms (audio OFF)");

        // Trigger an immediate check to ensure monitoring is active right away
        CheckAudioState(null, null!);

        // Start peak sampling and smoothing if enabled (for HTTP or WLED)
        if (volumeUpdateIntervalMs > 0 || wledUpdateIntervalMs > 0)
        {
            Logger.Log($"[DEBUG] Starting peak smoothing: sample every {PEAK_SAMPLE_RATE_MS}ms");

            // High-frequency sampling timer (10ms) to collect peak samples
            _peakSamplingTimer = new System.Timers.Timer(PEAK_SAMPLE_RATE_MS);
            _peakSamplingTimer.Elapsed += CollectPeakSample;
            _peakSamplingTimer.AutoReset = true;
            _peakSamplingTimer.Start();

            // HTTP volume timer
            if (volumeUpdateIntervalMs > 0)
            {
                Logger.Log($"[DEBUG] HTTP volume updates enabled: {volumeUpdateIntervalMs}ms");
                _volumeTimer = new System.Timers.Timer(volumeUpdateIntervalMs);
                _volumeTimer.Elapsed += SendAveragedPeak;
                _volumeTimer.AutoReset = true;
                _volumeTimer.Start();
            }

            // WLED timer (separate from HTTP)
            if (wledUpdateIntervalMs > 0)
            {
                Logger.Log($"[DEBUG] WLED updates enabled: {wledUpdateIntervalMs}ms");
                _wledTimer = new System.Timers.Timer(wledUpdateIntervalMs);
                _wledTimer.Elapsed += SendAveragedPeakWled;
                _wledTimer.AutoReset = true;
                _wledTimer.Start();
            }
        }
        else
        {
            Logger.Log($"[DEBUG] Peak monitoring disabled");
        }

        _isMonitoring = true;
        _lastState = false;
    }

    /// <summary>
    /// Stops monitoring all devices
    /// </summary>
    public void StopMonitoring()
    {
        if (_checkTimer != null)
        {
            _checkTimer.Stop();
            _checkTimer.Dispose();
            _checkTimer = null;
        }

        if (_peakSamplingTimer != null)
        {
            _peakSamplingTimer.Stop();
            _peakSamplingTimer.Dispose();
            _peakSamplingTimer = null;
        }

        if (_volumeTimer != null)
        {
            _volumeTimer.Stop();
            _volumeTimer.Dispose();
            _volumeTimer = null;
        }

        if (_wledTimer != null)
        {
            _wledTimer.Stop();
            _wledTimer.Dispose();
            _wledTimer = null;
        }

        lock (_peakLock)
        {
            _peakSamples.Clear();
        }

        _wasapiMonitor.StopMonitoring();

        _isMonitoring = false;
    }

    /// <summary>
    /// Manually check if audio is playing (for testing)
    /// </summary>
    public bool IsAudioPlaying()
    {
        return _wasapiMonitor.IsAudioPlaying();
    }

    /// <summary>
    /// Gets current audio peak level (0-100)
    /// </summary>
    public int GetPeakLevel()
    {
        return _wasapiMonitor.GetPeakLevel();
    }

    private void CheckAudioState(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_isMonitoring || _checkTimer == null)
            return;

        bool currentState = IsAudioPlaying();

        // Detect state change
        if (currentState != _lastState)
        {
            _lastState = currentState;
            Logger.Log($"[CHECK] Audio state changed to: {(currentState ? "ON" : "OFF")}");
            AudioStateChanged?.Invoke(this, new AudioStateChangedEventArgs(currentState));

            // Optimize check interval based on new state
            AdjustCheckInterval(currentState);
        }
    }

    /// <summary>
    /// Dynamically adjusts check interval based on audio state and peak monitoring
    /// </summary>
    private void AdjustCheckInterval(bool isAudioOn)
    {
        if (_checkTimer == null)
            return;

        double newInterval;

        if (!isAudioOn)
        {
            // Audio is OFF - check at base interval (e.g. 1000ms)
            newInterval = _baseCheckIntervalMs;
            Logger.Log($"[OPTIMIZE] Audio OFF - setting check interval to {newInterval}ms");
        }
        else
        {
            // Audio is ON
            if (_volumeUpdateIntervalMs > 0)
            {
                // Sending peak data - check at peak interval (e.g. 120ms)
                newInterval = _volumeUpdateIntervalMs;
                Logger.Log($"[OPTIMIZE] Audio ON with peak monitoring - setting check interval to {newInterval}ms");
            }
            else
            {
                // Not sending peak data - check at base interval (e.g. 1000ms)
                newInterval = _baseCheckIntervalMs;
                Logger.Log($"[OPTIMIZE] Audio ON without peak monitoring - setting check interval to {newInterval}ms");
            }
        }

        // Only change if different
        if (Math.Abs(_checkTimer.Interval - newInterval) > 0.001)
        {
            _checkTimer.Stop();
            _checkTimer.Interval = newInterval;
            _checkTimer.Start();
        }
    }

    /// <summary>
    /// Collects peak sample every 10ms for smoothing
    /// </summary>
    private void CollectPeakSample(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_isMonitoring || !IsAudioPlaying())
            return;

        int currentPeak = GetPeakLevel();

        lock (_peakLock)
        {
            _peakSamples.Add(currentPeak);
        }
    }

    /// <summary>
    /// Sends averaged peak level based on collected samples
    /// </summary>
    private void SendAveragedPeak(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_isMonitoring)
            return;

        // Only send if audio is playing
        if (!IsAudioPlaying())
        {
            Logger.Log($"[DEBUG] SendAveragedPeak: No audio, skipping");
            lock (_peakLock)
            {
                _peakSamples.Clear();
            }
            return;
        }

        int averagePeak;
        lock (_peakLock)
        {
            if (_peakSamples.Count == 0)
            {
                // No samples collected, use current peak
                averagePeak = GetPeakLevel();
            }
            else
            {
                // Calculate average of collected samples
                averagePeak = (int)_peakSamples.Average();
                Logger.Log($"[DEBUG] Averaged {_peakSamples.Count} samples: {averagePeak}");
            }

            _peakSamples.Clear();
        }

        Logger.Log($"[DEBUG] SendAveragedPeak: level={averagePeak}, subscribers={AudioVolumeChanged?.GetInvocationList().Length ?? 0}");
        AudioVolumeChanged?.Invoke(this, new AudioVolumeEventArgs(averagePeak));
    }

    /// <summary>
    /// Sends raw peak level for WLED (no averaging for better responsiveness)
    /// </summary>
    private void SendAveragedPeakWled(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_isMonitoring)
            return;

        // Only send if audio is playing
        if (!IsAudioPlaying())
        {
            return;
        }

        // Use raw peak level for WLED (no averaging) for better responsiveness and full dynamic range
        int peakLevel = GetPeakLevel();

        WledVolumeChanged?.Invoke(this, new AudioVolumeEventArgs(peakLevel));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopMonitoring();
        _wasapiMonitor.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public class AudioStateChangedEventArgs : EventArgs
{
    public bool IsPlaying { get; }

    public AudioStateChangedEventArgs(bool isPlaying)
    {
        IsPlaying = isPlaying;
    }
}

public class AudioVolumeEventArgs : EventArgs
{
    public int Level { get; }

    public AudioVolumeEventArgs(int level)
    {
        Level = level;
    }
}
