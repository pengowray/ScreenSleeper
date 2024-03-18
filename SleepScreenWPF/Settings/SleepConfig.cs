using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SleepScreenWPF.Settings;
public class SleepConfig {
    public string? Server { get; set; }
    public string? Topic { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? PayloadSleep { get; set; }

    

}
