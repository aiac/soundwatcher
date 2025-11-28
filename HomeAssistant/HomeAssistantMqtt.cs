using SoundWatcher.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace SoundWatcher.HomeAssistant;

/// <summary>
/// Home Assistant MQTT Discovery integration
/// Creates a media_player entity with states: playing, idle, unknown
/// </summary>
public class HomeAssistantMqtt : IDisposable
{
    private IMqttClient? _mqttClient;
    private readonly string _deviceName;
    private readonly string _uniqueId;
    private bool _disposed;
    private bool _isConnected;

    public HomeAssistantMqtt(string deviceName)
    {
        _deviceName = deviceName;
        _uniqueId = $"soundwatcher_{Environment.MachineName}".ToLower().Replace(" ", "_");
    }

    /// <summary>
    /// Connects to MQTT broker and publishes discovery config
    /// </summary>
    public async Task ConnectAsync(string broker, int port, string? username = null, string? password = null)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId($"soundwatcher_{Environment.MachineName}")
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(username))
        {
            optionsBuilder.WithCredentials(username, password);
        }

        var options = optionsBuilder.Build();

        _mqttClient.DisconnectedAsync += async e =>
        {
            Logger.Log($"[MQTT] Disconnected from broker: {e.Reason}");
            _isConnected = false;

            // Auto-reconnect after 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await _mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MQTT] Reconnection failed: {ex.Message}");
            }
        };

        try
        {
            Logger.Log($"[MQTT] Attempting to connect to {broker}:{port}...");
            var result = await _mqttClient.ConnectAsync(options);

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _isConnected = true;
                Logger.Log($"[MQTT] Successfully connected to {broker}:{port}");

                // Publish Home Assistant discovery configuration
                await PublishDiscoveryConfigAsync();

                // Publish initial state (idle = no audio)
                await PublishStateAsync("idle");
            }
            else
            {
                Logger.Log($"[MQTT] Connection failed with result: {result.ResultCode}");
                throw new Exception($"MQTT connection failed: {result.ResultCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[MQTT] Connection exception: {ex.Message}");
            Logger.Log($"[MQTT] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Publishes Home Assistant MQTT discovery configuration
    /// </summary>
    private async Task PublishDiscoveryConfigAsync()
    {
        var stateTopic = $"homeassistant/binary_sensor/{_uniqueId}/state";
        var configTopic = $"homeassistant/binary_sensor/{_uniqueId}/config";

        var config = new
        {
            name = _deviceName,
            unique_id = _uniqueId,
            state_topic = stateTopic,
            payload_on = "playing",
            payload_off = "idle",
            device_class = "sound",
            device = new
            {
                identifiers = new[] { _uniqueId },
                name = _deviceName,
                manufacturer = "SoundWatcher",
                model = "Audio Monitor",
                sw_version = "1.0.1"
            },
            icon = "mdi:speaker"
        };

        var payload = JsonSerializer.Serialize(config);
        Logger.Log($"[MQTT] Discovery config JSON: {payload}");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(configTopic)
            .WithPayload(payload)
            .WithRetainFlag()
            .Build();

        if (_mqttClient != null && _isConnected)
        {
            await _mqttClient.PublishAsync(message);
            Logger.Log($"[MQTT] Published discovery config to {configTopic}");
            Logger.Log($"[MQTT] State topic: {stateTopic}");
        }
        else
        {
            Logger.Log($"[MQTT] Cannot publish - client not connected (client={_mqttClient != null}, connected={_isConnected})");
        }
    }

    /// <summary>
    /// Publishes current audio state to Home Assistant
    /// States: playing, idle
    /// </summary>
    public async Task PublishStateAsync(string state)
    {
        if (_mqttClient == null || !_isConnected)
            return;

        var stateTopic = $"homeassistant/binary_sensor/{_uniqueId}/state";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(stateTopic)
            .WithPayload(state)
            .WithRetainFlag()
            .Build();

        try
        {
            await _mqttClient.PublishAsync(message);
            Logger.Log($"[MQTT] Published state: {state}");
        }
        catch (Exception ex)
        {
            Logger.Log($"[MQTT] Failed to publish state: {ex.Message}");
        }
    }

    public async void Dispose()
    {
        if (_disposed)
            return;

        if (_mqttClient != null && _isConnected)
        {
            // Publish offline state before disconnecting
            await PublishStateAsync("idle");
            await _mqttClient.DisconnectAsync();
            _mqttClient.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
