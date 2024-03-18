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

        public MainWindow() {
            InitializeComponent();
        }

        // on start
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // TODO: not actually called

            // TurnMonitorOn();
            LogThreadsafe("Loading...");
            ListenMQTTTask = ListenMQTT();
            //task.Wait(); // don't wait
            //MessageBox.Show("Loaded");
            //LogThreadsafe("Loaded.");
        }


        private void Connect_Button(object sender, RoutedEventArgs e) {
            LogThreadsafe("Connecting...");
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

        private async Task ListenMQTT() {
            try {
                SleepConfigManager configMgr = new SleepConfigManager();

                var config = await configMgr.RestoreConfigAsync();

                //todo: check for detauls
                //todo: better errors
                if (config.Server == null || config.Topic == null) {
                    LogThreadsafe("Please configure the MQTT server and topic in the settings.");
                    return;
                }

                if (config.Password == null) {
                    LogThreadsafe("Please configure the MQTT password in the settings.");
                    return;
                }

                LogThreadsafe($"Connecting to MQTT server: {config.Server}...");

                MqttClient = new MQTTClient(config.Server, config.Username, config.Password);
                MqttClient.MessageReceived += (s, e) => {
                    string payload = e.ApplicationMessage.ConvertPayloadToString();
                    //LogThreadsafe($"Topic:{e.ApplicationMessage.Topic}; Payload:{payload}; Tag:{e.Tag}");
                    LogThreadsafe($"MQTT: {e.ApplicationMessage.Topic} : {payload}");
                    if (!string.IsNullOrEmpty(WatchTopic) && WatchPayload != null) {
                        if (e.ApplicationMessage.Topic == WatchTopic && payload == WatchPayload) {
                            LogThreadsafe($"Sleep triggered.");
                            this.DoSafe(() => { 
                                Screen_Off(); 
                                LockScreenLib.LockScreen();}
                            );
                            
                        }
                    }

                    //Screen_Off_Button(s, null);
                };

                WatchTopic = config.Topic;
                WatchPayload = config.PayloadSleep;
                
                var result = await MqttClient.ConnectAsync();
                if (result.ResultCode == MQTTnet.Client.MqttClientConnectResultCode.Success) {
                    LogThreadsafe("Connected!");
                    await MqttClient.ListenToTopicAsync("$SYS/broker/version"); // show verison e.g. "mosquitto version 2.0.18"
                    await MqttClient.ListenToTopicAsync("homeassistant/status"); // "online"
                    await MqttClient.ListenToTopicAsync(config.Topic);
                    LogThreadsafe($"Listening to topic: {config.Topic}");
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