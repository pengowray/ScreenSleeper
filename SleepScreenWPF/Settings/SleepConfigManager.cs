using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SleepScreenWPF.Settings;
public class SleepConfigManager {

    public static readonly string _appName = "ScreenSleeper";
    public readonly string _configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _appName);
    public readonly string _configFile = "mqttConfig.json";

    public SleepConfigManager() {
        if (!Directory.Exists(_configFolder)) {
            Directory.CreateDirectory(_configFolder);
        }
    }

    public async Task SaveConfigAsync(SleepConfig config) {
        var filePath = Path.Combine(_configFolder, _configFile);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<SleepConfig> RestoreConfigAsync() {
        var filePath = Path.Combine(_configFolder, _configFile);
        if (!File.Exists(filePath)) {
            return CreateDefaultConfig();
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<SleepConfig>(json);
    }

    private SleepConfig CreateDefaultConfig() {

        var defaultConfig = new SleepConfig {
            ConfigVersion = "1.0",
            Server = "mqtt.example.com", // "mqtt://yourdefaultserver",
            Protocol = "mqtt", // "mqtt", "mqtts", "ws", "wss"
            Username = "screensleeper",
            Password = "yourpassword",
            Port = "auto-ha",
            AutoConnect = false,
            AllowBadSSL = false,
            SocketPath = "/",
            Triggers = new[] {
                new TriggerConfig {
                    Topic = "myroom/thispc/sleep/set",
                    Payload = "on",
                    Action = "Lock;ScreenOff",
                }
            }
        };

        //TODO: wildcard support for topic, # and +
        //TODO: regex matching for payload
        //TODO: jq for payload
        //TODO: heartbeat / homie

        // Optionally, save the default config for future use
        SaveConfigAsync(defaultConfig).Wait();

        return defaultConfig;
    }
}
