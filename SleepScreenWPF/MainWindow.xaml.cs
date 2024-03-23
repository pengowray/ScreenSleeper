using MQTT;
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
        }

        // on start
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // TODO: not actually called

            // TurnMonitorOn();
            LogThreadsafe("Loading...");

            ListenMQTTTask = ListenMQTT(onlyConnectIfAutoTrue: true);
            //task.Wait(); // don't wait
            //MessageBox.Show("Loaded");
            //LogThreadsafe("Loaded.");
        }


        private void Connect_Button(object sender, RoutedEventArgs e) {
            ListenMQTTTask = ListenMQTT();
            //ListenMQTTTask.Wait();
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
                

                SleepConfigManager configMgr = new SleepConfigManager();
                Config = await configMgr.RestoreConfigAsync();

                if (onlyConnectIfAutoTrue && (!Config.AutoConnect.HasValue || !Config.AutoConnect.Value)) {
                    // not auto connecting
                    LogThreadsafe("Press connect to listen to MQTT server.");
                    return;
                }

                //todo: check for detauls
                //todo: better errors
                if (Config.Server == null) {
                    LogThreadsafe("Please configure the MQTT server in the settings.");
                    return;
                }

                if (Config.Username == null) {
                    LogThreadsafe("Please configure the MQTT username in the settings.");
                    return;
                }

                if (Config.Password == null) {
                    LogThreadsafe("Please configure the MQTT password in the settings.");
                    return;
                }

                LogThreadsafe($"Connecting to MQTT server: {Config.Server}...");

                MqttClient = new MQTTClient(Config.Server, Config.Username, Config.Password);

                MqttClient.MessageReceived += (s, e) => {
                    string payload = e.ApplicationMessage.ConvertPayloadToString();
                    //LogThreadsafe($"Topic:{e.ApplicationMessage.Topic}; Payload:{payload}; Tag:{e.Tag}");
                    LogThreadsafe($"MQTT: {e.ApplicationMessage.Topic} → '{payload}'");

                    if (Config == null) return;

                    bool hasAction = Config.Action != null && Config.Action.Length > 0;
                    if (!hasAction) return;
                    
                    var matchingActions = Config.Action.Where(a => a.Topic == e.ApplicationMessage.Topic && a.Payload == payload).ToArray();
                    if (matchingActions.Length > 0) {
                        foreach (var action in matchingActions) {
                            LogThreadsafe($"Action: {action.Action}; Topic: {action.Topic}; Payload: {action.Payload}");
                            var actions = action?.Action?.Split(';');
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
                                    default:
                                        if (string.IsNullOrEmpty(act)) break;
                                        LogThreadsafe($"Unknown action: {act}");
                                        break;
                                }
                            }
                        }
                    }   
                };

                bool hasAction = Config.Action != null && Config.Action.Length > 0;
                if (!hasAction) {
                    LogThreadsafe("No action(s) configured.");
                }

                var result = await MqttClient.ConnectAsync();
                if (result.ResultCode == MQTTnet.Client.MqttClientConnectResultCode.Success) {
                    LogThreadsafe("Connected!");
                    await MqttClient.ListenToTopicAsync("$SYS/broker/version"); // show verison e.g. "mosquitto version 2.0.18"
                    await MqttClient.ListenToTopicAsync("homeassistant/status"); // "online"
                    if (hasAction) {
                        var topics = Config.Action.Select(a => a.Topic).Distinct().ToArray();
                        foreach (var topic in topics) {
                            await MqttClient.ListenToTopicAsync(topic);
                            LogThreadsafe($"Listening to topic: {topic}");
                        }
                        LogThreadsafe("Action list:");
                        int count = 0;
                        foreach (var action in Config?.Action ?? []) {
                            LogThreadsafe($"{++count}. when topic '{action.Topic}' with payload '{action.Payload}' do '{action.Action}'");
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
            //TODO: add datestamp?

            Log.AppendTextSafe(text + "\n");
            
        }


    }
}