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
        // Set your default values here
        var defaultConfig = new SleepConfig {
            ConfigVersion = "1.0",
            Server = "mqtt.example.com", // "mqtt://yourdefaultserver",
            //Port = "Default", // "Default" or "1883" or "8883" or "8080" or "443"
            //Protocol = "mqtt", // "mqtt", "mqtts", "ws", "wss"
            Username = "screensleeper",
            Password = "hackme",
            AutoConnect = false,
            Action = new[] {
                new ActionConfig {
                    Topic = "your/topic",
                    Payload = "sleep",
                    Action = "Lock;ScreenOff",
                }
            }
        };

        //TODO: port number (defaults to 1883 = no ssl, no websocket)
        //TODO: wildcard support for topic, # and +
        //TODO: regex matching for payload

        // Optionally, save the default config for future use
        SaveConfigAsync(defaultConfig).Wait();

        return defaultConfig;
    }
}
