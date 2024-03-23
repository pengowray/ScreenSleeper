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
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? AutoConnect { get; set; }
    public ActionConfig[]? Action { get; set; }
}

public class ActionConfig {
    public string? Action { get; set; } // default: "Lock,ScreenOff"
    public string? Topic { get; set; }
    public string? Payload { get; set; }
}
