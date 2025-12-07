using SoundWatcher.Logging;
using System.Net;
using System.Net.Sockets;

namespace SoundWatcher.Network;

/// <summary>
/// Sends audio-reactive color data to WLED devices via UDP
/// </summary>
public class WledUdpSender : IDisposable
{
    private readonly UdpClient _udpClient;
    private IPEndPoint? _endpoint;
    private bool _disposed;
    private const int DEFAULT_PORT = 21324;
    private const byte DRGB_PROTOCOL = 2; // DRGB protocol type
    private const byte TIMEOUT_SECONDS = 1; // 1 second timeout
    private VuMeterBallistics? _vuMeter;

    public WledUdpSender()
    {
        _udpClient = new UdpClient();
    }

    /// <summary>
    /// Sets the VU meter decay time for ballistics
    /// </summary>
    /// <param name="decayTimeMs">Decay time in milliseconds (50-500ms typical)</param>
    public void SetVuMeterDecay(int decayTimeMs)
    {
        _vuMeter = new VuMeterBallistics(decayTimeMs);
    }

    /// <summary>
    /// Sets the target WLED device
    /// </summary>
    /// <param name="hostOrIp">Hostname or IP address</param>
    /// <param name="port">UDP port (default 21324)</param>
    public void SetTarget(string hostOrIp, int port = DEFAULT_PORT)
    {
        try
        {
            // Try to parse as IP address first
            if (IPAddress.TryParse(hostOrIp, out var ipAddress))
            {
                _endpoint = new IPEndPoint(ipAddress, port);
            }
            else
            {
                // Resolve hostname
                var addresses = Dns.GetHostAddresses(hostOrIp);
                if (addresses.Length > 0)
                {
                    _endpoint = new IPEndPoint(addresses[0], port);
                }
                else
                {
                    throw new ArgumentException($"Could not resolve hostname: {hostOrIp}");
                }
            }

            Logger.Log($"[WLED] Target set to {_endpoint.Address}:{_endpoint.Port}");
        }
        catch (Exception ex)
        {
            Logger.Log($"[WLED] Error setting target: {ex.Message}");
            _endpoint = null;
            throw;
        }
    }

    /// <summary>
    /// Sends audio peak level to WLED device with specified visualization mode
    /// </summary>
    /// <param name="peakLevel">Peak level (0-100)</param>
    /// <param name="ledCount">Number of LEDs to control</param>
    /// <param name="mode">Visualization mode: 0=gradient, 1=left-to-right, 2=center-out, 3=brightness</param>
    /// <param name="color">Hex color string (e.g. "#FF0000" for red)</param>
    public void SendAudioPeak(int peakLevel, int ledCount = 60, int mode = 0, string color = "#FF0000")
    {
        if (_endpoint == null || _disposed)
            return;

        try
        {
            // Apply VU meter ballistics (fast attack, slow decay)
            int displayLevel = peakLevel;
            if (_vuMeter != null)
            {
                displayLevel = _vuMeter.Update(peakLevel);
            }

            // Create UDP packet: [Protocol][Timeout][R1][G1][B1][R2][G2][B2]...
            // Packet size: 2 + (ledCount * 3) bytes
            var packet = new byte[2 + (ledCount * 3)];

            packet[0] = DRGB_PROTOCOL;  // DRGB protocol
            packet[1] = TIMEOUT_SECONDS; // Timeout in seconds

            // Fill packet based on visualization mode
            switch (mode)
            {
                case 0: // Gradient mode (original behavior)
                    FillGradientMode(packet, displayLevel, ledCount);
                    break;
                case 1: // Left to right
                    FillLeftToRightMode(packet, displayLevel, ledCount, color);
                    break;
                case 2: // Center out
                    FillCenterOutMode(packet, displayLevel, ledCount, color);
                    break;
                case 3: // Brightness
                    FillBrightnessMode(packet, displayLevel, ledCount, color);
                    break;
                default:
                    FillGradientMode(packet, displayLevel, ledCount);
                    break;
            }

            _udpClient.Send(packet, packet.Length, _endpoint);
        }
        catch (Exception ex)
        {
            Logger.Log($"[WLED] Error sending peak: {ex.Message}");
        }
    }

    /// <summary>
    /// Mode 0: Gradient - all LEDs show same color based on peak (Blue→Cyan→Green→Yellow→Red)
    /// </summary>
    private void FillGradientMode(byte[] packet, int peakLevel, int ledCount)
    {
        var (r, g, b) = PeakToColor(peakLevel);

        for (int i = 0; i < ledCount; i++)
        {
            int offset = 2 + (i * 3);
            packet[offset] = r;
            packet[offset + 1] = g;
            packet[offset + 2] = b;
        }
    }

    /// <summary>
    /// Mode 1: Left to Right VU Meter - more peak = more LEDs light up from left (LED 0)
    /// Extended dynamic range: 0-30% peak = first LED only, 30-100% peak = spreads across remaining LEDs
    /// </summary>
    private void FillLeftToRightMode(byte[] packet, int peakLevel, int ledCount, string hexColor)
    {
        var (r, g, b) = ParseHexColor(hexColor);

        int litLeds;
        if (peakLevel == 0)
        {
            litLeds = 0; // No LEDs lit
        }
        else if (peakLevel <= 30)
        {
            litLeds = 1; // Only first LED lit for 0-30%
        }
        else
        {
            // Map 30-100% to remaining LEDs (1 to ledCount)
            // 30% = 1 LED, 100% = all LEDs
            double normalizedPeak = (peakLevel - 30.0) / 70.0; // 0.0 to 1.0
            litLeds = 1 + (int)Math.Round((ledCount - 1) * normalizedPeak);
        }

        for (int i = 0; i < ledCount; i++)
        {
            int offset = 2 + (i * 3);
            if (i < litLeds)
            {
                // LED is lit
                packet[offset] = r;
                packet[offset + 1] = g;
                packet[offset + 2] = b;
            }
            else
            {
                // LED is off
                packet[offset] = 0;
                packet[offset + 1] = 0;
                packet[offset + 2] = 0;
            }
        }
    }

    /// <summary>
    /// Mode 2: Center Out VU Meter - more peak = more LEDs light up from center outwards
    /// Extended dynamic range: 0-30% peak = center LED only, 30-100% peak = spreads outward to edges
    /// </summary>
    private void FillCenterOutMode(byte[] packet, int peakLevel, int ledCount, string hexColor)
    {
        var (r, g, b) = ParseHexColor(hexColor);
        int center = ledCount / 2;

        int litLedsPerSide;
        if (peakLevel == 0)
        {
            litLedsPerSide = -1; // No LEDs lit
        }
        else if (peakLevel <= 30)
        {
            litLedsPerSide = 0; // Only center LED lit for 0-30%
        }
        else
        {
            // Map 30-100% to full spread (0 to ledCount/2)
            // 30% = center only, 100% = full strip
            double normalizedPeak = (peakLevel - 30.0) / 70.0; // 0.0 to 1.0
            litLedsPerSide = (int)Math.Round((ledCount / 2.0) * normalizedPeak);
        }

        for (int i = 0; i < ledCount; i++)
        {
            int offset = 2 + (i * 3);
            int distanceFromCenter = Math.Abs(i - center);

            if (distanceFromCenter <= litLedsPerSide)
            {
                // LED is lit
                packet[offset] = r;
                packet[offset + 1] = g;
                packet[offset + 2] = b;
            }
            else
            {
                // LED is off
                packet[offset] = 0;
                packet[offset + 1] = 0;
                packet[offset + 2] = 0;
            }
        }
    }

    /// <summary>
    /// Mode 3: Brightness - all LEDs on, brightness controlled by peak
    /// 0% peak = LEDs off, 50% peak = half brightness, 100% peak = full brightness
    /// </summary>
    private void FillBrightnessMode(byte[] packet, int peakLevel, int ledCount, string hexColor)
    {
        var (r, g, b) = ParseHexColor(hexColor);

        // Scale color by peak level (0-100%)
        byte scaledR = (byte)(r * peakLevel / 100);
        byte scaledG = (byte)(g * peakLevel / 100);
        byte scaledB = (byte)(b * peakLevel / 100);

        for (int i = 0; i < ledCount; i++)
        {
            int offset = 2 + (i * 3);
            packet[offset] = scaledR;
            packet[offset + 1] = scaledG;
            packet[offset + 2] = scaledB;
        }
    }

    /// <summary>
    /// Parses hex color string (e.g. "#FF0000") to RGB bytes
    /// </summary>
    private (byte r, byte g, byte b) ParseHexColor(string hexColor)
    {
        try
        {
            // Remove # if present
            hexColor = hexColor.TrimStart('#');

            if (hexColor.Length == 6)
            {
                byte r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                byte g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                byte b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                return (r, g, b);
            }
        }
        catch
        {
            Logger.Log($"[WLED] Invalid hex color '{hexColor}', using default red");
        }

        // Default to red if parsing fails
        return (255, 0, 0);
    }

    /// <summary>
    /// Converts audio peak level to RGB color
    /// Low peak = blue/green, high peak = yellow/red
    /// </summary>
    private (byte r, byte g, byte b) PeakToColor(int peak)
    {
        // Clamp peak to 0-100
        peak = Math.Clamp(peak, 0, 100);

        // Simple color gradient:
        // 0-25: Blue to Cyan
        // 25-50: Cyan to Green
        // 50-75: Green to Yellow
        // 75-100: Yellow to Red

        if (peak < 25)
        {
            // Blue (0,0,255) to Cyan (0,255,255)
            var t = peak / 25f;
            return (0, (byte)(t * 255), 255);
        }
        else if (peak < 50)
        {
            // Cyan (0,255,255) to Green (0,255,0)
            var t = (peak - 25) / 25f;
            return (0, 255, (byte)(255 * (1 - t)));
        }
        else if (peak < 75)
        {
            // Green (0,255,0) to Yellow (255,255,0)
            var t = (peak - 50) / 25f;
            return ((byte)(t * 255), 255, 0);
        }
        else
        {
            // Yellow (255,255,0) to Red (255,0,0)
            var t = (peak - 75) / 25f;
            return (255, (byte)(255 * (1 - t)), 0);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _udpClient?.Dispose();
            _disposed = true;
        }
    }
}
