﻿using MQTT;
using MQTTnet;
using SleepScreenWPF.Settings;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

namespace SleepScreenWPF {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private MQTTClient? MqttClient;
        private Task? ListenMQTTTask;

        // sleep when topic = payload, e.g. Topic:habitat/light; Payload:offline
        string? WatchTopic;
        string? WatchPayload;

        SleepConfig Config;

        public MainWindow() {
            InitializeComponent();

            // shortcuts for font size
            this.KeyDown += MainWindow_KeyDown;
            this.PreviewMouseWheel += MainWindow_PreviewMouseWheel;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                switch (e.Key) {
                    case Key.OemPlus:
                    case Key.Add:
                        IncreaseFontSize();
                        e.Handled = true;
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        DecreaseFontSize();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                if (e.Delta > 0) {
                    IncreaseFontSize();
                } else if (e.Delta < 0) {
                    DecreaseFontSize();
                }
                e.Handled = true;
            }
        }

        private void IncreaseFontSize() {
            if (Log.FontSize < 36) {
                Log.FontSize += 2;
            }
        }

        private void DecreaseFontSize() {
            if (Log.FontSize > 8) {
                Log.FontSize -= 2;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // TurnMonitorOn();
            LogThreadsafe("Loading...");
            ListenMQTTTask = ListenMQTT(onlyConnectIfAutoTrue: true);
        }


        private void Connect_Button(object sender, RoutedEventArgs e) {
            ListenMQTTTask = ListenMQTT();
            //ListenMQTTTask.Wait();
        }

        private void Disconnect_Button(object sender, RoutedEventArgs e) {
            LogThreadsafe("Disconnecting...");
            MqttClient?.DisconnectAsync().Wait();
        }

        private void TurnMonitorOn() {
            SleepLib.MonitorOn();
        }

        private void Screen_Off_Button(object sender, RoutedEventArgs e) {
            Screen_Off();
        }

        private void Screen_Off() {
            // Obtain the window handle for this Window instance
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr windowHandle = helper.Handle;

            SleepLib.MonitorOff(windowHandle);
        }


        private void Lock_Button(object sender, RoutedEventArgs e) {
            LockScreenLib.LockScreen();
        }

        private async Task ListenMQTT(bool onlyConnectIfAutoTrue = false) {
            try {
                if (MqttClient != null) {
                    LogThreadsafe("Already connected. Disconnecting...");
                    MqttClient.Dispose();
                    MqttClient = null;
                }

                SleepConfigManager configMgr = SleepConfigManager.Instance;
                Config = await configMgr.RestoreConfigAsync();

                if (onlyConnectIfAutoTrue && (!Config.AutoConnect.HasValue || !Config.AutoConnect.Value)) {
                    // not auto connecting
                    LogThreadsafe("Press connect to listen to MQTT server.");
                    return;
                }

                //todo: check for detauls
                //todo: better errors
                if (Config.Server == null || Config.Server == "example.com" || Config.Server.EndsWith(".example.com")) {
                    LogThreadsafe("Please configure the server for MQTT in the config.");
                    return;
                }

                if (Config.Username == null) {
                    LogThreadsafe("Please configure the MQTT username in the config.");
                    return;
                }

                if (Config.Password == null) {
                    LogThreadsafe("Please configure the MQTT password in the config.");
                    return;
                }

                LogThreadsafe($"Connecting to MQTT server {Config.ParseFullUrl()}...");

                MqttClient = new MQTTClient(Config);
                MqttClient.StatusEvent += (s, e) => {
                    LogThreadsafe(e);
                };
                MqttClient.MessageReceived += (s, e) => {
                    string topic = e.ApplicationMessage.Topic;
                    string payload = e.ApplicationMessage.ConvertPayloadToString();
                    //LogThreadsafe($"Topic:{e.ApplicationMessage.Topic}; Payload:{payload}; Tag:{e.Tag}");
                    LogThreadsafe($"MQTT: {topic} → '{payload}'");

                    if (string.IsNullOrWhiteSpace(topic)) {
                        LogThreadsafe($"Topic is empty. Ignoring message.");
                        return;
                    }

                    if (Config == null) return;

                    bool hasAction = Config?.Triggers != null && Config.Triggers.Length > 0;
                    if (!hasAction) return;
                    
                    var matchingActions = Config.Triggers.Where(a => a.Topic == e.ApplicationMessage.Topic && a.Payload == payload).ToArray();
                    if (matchingActions.Length > 0) {
                        foreach (var action in matchingActions) {
                            LogThreadsafe($"Action: {action.Action}; Topic: {action.Topic}; Payload: {action.Payload}");
                            var actions = action?.Action?.Split(';').Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim());
                            foreach (var act in actions ?? []) {
                                switch (act) {
                                    case "Lock":
                                        LogThreadsafe("Lock triggered.");
                                        this.DoSafe(() => LockScreenLib.LockScreen());
                                        break;
                                    case "ScreenOff":
                                        LogThreadsafe("ScreenOff triggered.");
                                        this.DoSafe(() => Screen_Off());
                                        break;
                                    case "ScreenOn":
                                        LogThreadsafe("ScreenOn triggered.");
                                        this.DoSafe(() => SleepLib.MonitorOn());
                                        break;
                                    default:
                                        if (string.IsNullOrEmpty(act)) break;
                                        LogThreadsafe($"Unknown action: {act}");
                                        break;
                                }
                            }
                        }
                    }   
                };

                bool hasAction = Config.Triggers != null && Config.Triggers.Length > 0;
                if (!hasAction) {
                    LogThreadsafe("No action(s) configured.");
                }

                var result = await MqttClient.ConnectAsync();
                if (result.ResultCode == MQTTnet.Client.MqttClientConnectResultCode.Success) {
                    //LogThreadsafe("Connected!"); // redundant
                    await MqttClient.ListenToTopicAsync("$SYS/broker/version"); // show verison e.g. "mosquitto version 2.0.18"
                    await MqttClient.ListenToTopicAsync("homeassistant/status"); // show "online"
                    if (hasAction) {
                        var topics = Config.Triggers.Select(a => a.Topic).Distinct().Where(topic => !string.IsNullOrWhiteSpace(topic)).ToArray();
                        foreach (var topic in topics) {
                            await MqttClient.ListenToTopicAsync(topic);
                            LogThreadsafe($"Listening to topic: '{topic}'");
                        }
                        LogThreadsafe("Triggers:");
                        int count = 0;
                        foreach (var action in Config?.Triggers ?? []) {
                            LogThreadsafe($"{++count}. when topic '{action.Topic}' with payload '{action.Payload}' do '{action.Action}'");
                        }
                        if (count==0) {
                            LogThreadsafe("(none)");
                        }
                    }
                } else {
                    LogThreadsafe("Failed to connect");
                }

            } catch (Exception ex) {
                LogThreadsafe($"Error: {ex.Message}");
            }
        }

        private void LogWriteLine(string text) {
            Log.Text += text + "\n";
        }

        private void LogThreadsafe(string text) {
            string dateStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss (zzz)");
            Log.AppendTextSafe($"{dateStamp}: {text}\n");
        }

        private void Show_Config_Folder_Explorer(object sender, RoutedEventArgs e) {
            SleepConfigManager configMgr = SleepConfigManager.Instance; //note: creates the folder (though already done earlier)
            string dir = configMgr._configFolder;
            string file = configMgr._configFile;
            //string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ScreenSleeper");

            if (System.IO.Directory.Exists(dir)) { // safety check
                // show with file selected
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, file))) {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{System.IO.Path.Combine(dir, file)}\"");
                } else {
                    System.Diagnostics.Process.Start("explorer.exe", dir);
                }
            } else {
                LogThreadsafe($"Config folder not found: '{dir}'");
            }
        }

    }
}