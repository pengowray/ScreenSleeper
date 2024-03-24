using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SleepScreenWPF.Settings;
public class SleepConfig {
    public string? ConfigVersion { get; set; }
    public string? Server { get; set; }
    public string? Protocol { get; set; } // mqtt, mqtts, ws, wss
    public string? Port { get; set; } // default based on protocol; number or "auto" or "default"
    public string? SocketPath { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? AutoConnect { get; set; }
    public bool? AllowBadSSL { get; set; }
    public TriggerConfig[]? Triggers { get; set; }

    public int ParsePort() {
        if (int.TryParse(Port, out int p) && p > 0) {
            return p;
        } else {
            if (Port == "auto-ha") {
                return ParseProtocol() switch {
                    "mqtt" => 1883,
                    "mqtts" => 8883, // Normal MQTT over SSL/TLS
                    "ws" => 1884, // MQTT over WebSocket
                    "wss" => 8884, // MQTT over WebSocket with SSL/TLS
                    _ => 1883
                };
            } else { 
                return ParseProtocol() switch {
                    "mqtt" => 1883,
                    "mqtts" => 8883,
                    "ws" => 80,
                    "wss" => 443,
                    _ => 1883
                };
            }
        }
    }

    public string ParseProtocol() {
        string protocol = Protocol?.ToLowerInvariant()?.Trim() ?? "mqtt";
        if (protocol == "wss" || protocol == "ws" || protocol == "mqtt" || protocol == "mqtts") {
            return protocol;
        } else {
            return "mqtt";
        }
    }

    public string ParseFullUrl() {
        var protocol = ParseProtocol();
        string url = $"{protocol}://{Server}:{ParsePort()}";
        if (protocol == "ws" || protocol == "wss") {
            url += SocketPath ?? "";
        }

        return url;
    }
}

public class TriggerConfig {
    public string? Topic { get; set; }
    public string? Payload { get; set; }
    public string? Action { get; set; } // default: "Lock;ScreenOff"
}

