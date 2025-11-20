using SoundWatcher.Logging;
using System.Runtime.InteropServices;
using static SoundWatcher.Audio.WasapiInterop;

namespace SoundWatcher.Audio;

/// <summary>
/// Monitors WASAPI audio devices for sound activity
/// </summary>
public class WasapiMonitor : IDisposable
{
    private IMMDeviceEnumerator? _deviceEnumerator;
    private readonly List<AudioDevice> _monitoredDevices = new();
    private bool _disposed;
    private float _audioThresholdPercent = 2.0f; // Default 2%

    public WasapiMonitor()
    {
        InitializeDeviceEnumerator();
    }

    private void InitializeDeviceEnumerator()
    {
        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator);
            _deviceEnumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(enumeratorType!);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize WASAPI device enumerator", ex);
        }
    }

    /// <summary>
    /// Gets all available audio devices (both output and input)
    /// </summary>
    public List<AudioDeviceInfo> GetAvailableDevices()
    {
        if (_deviceEnumerator == null)
            throw new InvalidOperationException("Device enumerator not initialized");

        var devices = new List<AudioDeviceInfo>();

        // Get both Render (output) and Capture (input) devices
        var dataFlows = new[] { EDataFlow.eRender, EDataFlow.eCapture };
        var flowNames = new[] { "Output", "Input" };

        for (int f = 0; f < dataFlows.Length; f++)
        {
            try
            {
                _deviceEnumerator.EnumAudioEndpoints(dataFlows[f], DeviceState.Active, out var deviceCollection);
                deviceCollection.GetCount(out var count);

                for (uint i = 0; i < count; i++)
                {
                    deviceCollection.Item(i, out var device);
                    device.GetId(out var id);

                    var name = GetDeviceFriendlyName(device);

                    devices.Add(new AudioDeviceInfo
                    {
                        Id = id,
                        Name = $"[{flowNames[f]}] {name}",
                        IsInputDevice = (dataFlows[f] == EDataFlow.eCapture)
                    });

                    Marshal.ReleaseComObject(device);
                }

                Marshal.ReleaseComObject(deviceCollection);
            }
            catch { }
        }

        return devices;
    }


    /// <summary>
    /// Gets the default audio output device
    /// </summary>
    public AudioDeviceInfo? GetDefaultDevice()
    {
        if (_deviceEnumerator == null)
            return null;

        try
        {
            _deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
            device.GetId(out var id);
            var name = GetDeviceFriendlyName(device);

            var deviceInfo = new AudioDeviceInfo
            {
                Id = id,
                Name = name
            };

            Marshal.ReleaseComObject(device);
            return deviceInfo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the audio detection threshold (0-100 percent)
    /// </summary>
    public void SetAudioThreshold(float thresholdPercent)
    {
        _audioThresholdPercent = Math.Clamp(thresholdPercent, 0f, 100f);
        Logger.Log($"[WASAPI] Audio threshold set to {_audioThresholdPercent}%");
    }

    /// <summary>
    /// Starts monitoring specified devices for audio activity
    /// </summary>
    public void StartMonitoring(IEnumerable<AudioDeviceInfo> devices)
    {
        StopMonitoring();

        if (_deviceEnumerator == null)
            return;

        foreach (var deviceInfo in devices)
        {
            try
            {
                _deviceEnumerator.GetDevice(deviceInfo.Id, out var device);

                IAudioMeterInformation? meter = null;
                IAudioClient? audioClient = null;

                // For input devices, we need to start silent capture to activate the meter
                if (deviceInfo.IsInputDevice)
                {
                    Logger.Log($"[WASAPI] Starting silent capture for input device: {deviceInfo.Name}");

                    try
                    {
                        // Get IAudioClient
                        var clientIid = IID_IAudioClient;
                        device.Activate(ref clientIid, 0, IntPtr.Zero, out var clientObj);
                        audioClient = (IAudioClient)clientObj;

                        // Get mix format
                        audioClient.GetMixFormat(out var formatPtr);

                        // Initialize capture session (silent - we won't read the buffer)
                        var sessionGuid = Guid.Empty;
                        audioClient.Initialize(
                            AudioClientShareMode.Shared,
                            AudioClientStreamFlags.None,
                            10000000, // 1 second buffer
                            0,
                            formatPtr,
                            ref sessionGuid);

                        // Free format
                        Marshal.FreeCoTaskMem(formatPtr);

                        // Start capture (this activates the meter)
                        audioClient.Start();

                        // Now get the meter - it should work because capture is active
                        var meterIid = IID_IAudioMeterInformation;
                        device.Activate(ref meterIid, 0, IntPtr.Zero, out var meterObj);
                        meter = (IAudioMeterInformation)meterObj;

                        Logger.Log($"[WASAPI] Successfully started silent capture for input device: {deviceInfo.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[WASAPI] Failed to start silent capture for input device {deviceInfo.Name}: {ex.Message}");
                        if (audioClient != null)
                            Marshal.ReleaseComObject(audioClient);
                        Marshal.ReleaseComObject(device);
                        continue;
                    }
                }
                else
                {
                    // For output devices, meter works directly
                    var meterIid = IID_IAudioMeterInformation;
                    device.Activate(ref meterIid, 0, IntPtr.Zero, out var meterObj);
                    meter = (IAudioMeterInformation)meterObj;
                }

                _monitoredDevices.Add(new AudioDevice
                {
                    Id = deviceInfo.Id,
                    Device = device,
                    MeterInfo = meter,
                    AudioClient = audioClient,
                    IsInputDevice = deviceInfo.IsInputDevice
                });

                Logger.Log($"[WASAPI] Successfully initialized monitoring for device: {deviceInfo.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[WASAPI] Failed to initialize device {deviceInfo.Name}: {ex.Message}");
            }
        }

        Logger.Log($"[WASAPI] Monitoring {_monitoredDevices.Count} device(s)");

        if (_monitoredDevices.Count == 0)
        {
            Logger.Log($"[WASAPI] WARNING: No devices are being monitored!");
        }
    }

    /// <summary>
    /// Checks if any monitored device is currently playing audio
    /// </summary>
    public bool IsAudioPlaying()
    {
        foreach (var device in _monitoredDevices)
        {
            try
            {
                if (device.MeterInfo != null)
                {
                    device.MeterInfo.GetPeakValue(out var peak);

                    // Convert threshold from percentage (0-100) to float (0.0-1.0)
                    float threshold = _audioThresholdPercent / 100f;

                    if (peak > threshold)
                        return true;
                }
            }
            catch
            {
                // Skip devices that fail to read
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the current peak audio level from all monitored devices (0-100)
    /// </summary>
    public int GetPeakLevel()
    {
        float maxPeak = 0;

        foreach (var device in _monitoredDevices)
        {
            try
            {
                if (device.MeterInfo != null)
                {
                    device.MeterInfo.GetPeakValue(out var peak);
                    if (peak > maxPeak)
                        maxPeak = peak;
                }
            }
            catch
            {
                // Skip devices that fail to read
            }
        }

        // Convert from 0.0-1.0 to 0-100
        return (int)(maxPeak * 100);
    }

    /// <summary>
    /// Stops monitoring all devices
    /// </summary>
    public void StopMonitoring()
    {
        foreach (var device in _monitoredDevices)
        {
            try
            {
                // Stop audio client if this is an input device
                if (device.AudioClient != null)
                {
                    try
                    {
                        device.AudioClient.Stop();
                        Marshal.ReleaseComObject(device.AudioClient);
                    }
                    catch { }
                }

                if (device.MeterInfo != null)
                    Marshal.ReleaseComObject(device.MeterInfo);
                if (device.Device != null)
                    Marshal.ReleaseComObject(device.Device);
            }
            catch { }
        }

        _monitoredDevices.Clear();
    }

    private string GetDeviceFriendlyName(IMMDevice device)
    {
        try
        {
            device.OpenPropertyStore(0, out var propertyStore);
            var key = PropertyKeys.PKEY_Device_FriendlyName;
            propertyStore.GetValue(ref key, out var value);

            var name = Marshal.PtrToStringUni(value.pwszVal) ?? "Unknown Device";

            Marshal.ReleaseComObject(propertyStore);

            return name;
        }
        catch
        {
            return "Unknown Device";
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopMonitoring();

        if (_deviceEnumerator != null)
        {
            Marshal.ReleaseComObject(_deviceEnumerator);
            _deviceEnumerator = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private class AudioDevice
    {
        public string Id { get; set; } = string.Empty;
        public IMMDevice? Device { get; set; }
        public IAudioMeterInformation? MeterInfo { get; set; }
        public IAudioClient? AudioClient { get; set; }
        public bool IsInputDevice { get; set; }
    }
}

public class AudioDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsInputDevice { get; set; } = false;
}
