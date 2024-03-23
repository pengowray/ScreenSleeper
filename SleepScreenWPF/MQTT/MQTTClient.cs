using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MQTT {
    public class MQTTClient : IDisposable {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;
        public event EventHandler<string> StatusEvent;

        int RetryAttempts = 0;
        int MaxRetry = 10;

        public MQTTClient(string brokerAddress, string username, string password) {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            // Setup message handling
            _mqttClient.ApplicationMessageReceivedAsync += e => {
                // Invoke the event
                MessageReceived?.Invoke(this, e);
                return Task.CompletedTask;
            };

            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerAddress)
                .WithClientId(username)
                //.WithAuthentication("password", Encoding.UTF8.GetBytes(password))
                .WithCredentials(username, password)
                .Build();

            //TODO: configurable reconnection
            _mqttClient.DisconnectedAsync += async (args) => {
                StatusEvent?.Invoke(this, "### DISCONNECTED FROM SERVER ###");
                if (RetryAttempts++ > MaxRetry) {
                    StatusEvent?.Invoke(this, "### MAX RETRY ATTEMPTS REACHED ###");
                    return;
                } else {
                    StatusEvent?.Invoke(this, $"### RECONNECTING {RetryAttempts}/{MaxRetry} ###");
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
                try {
                    await _mqttClient.ConnectAsync(_mqttClientOptions);
                } catch {
                    StatusEvent?.Invoke(this, "### RECONNECTION ERROR ###");
                }
            };
        }

        public async Task<MqttClientConnectResult> ConnectAsync() {
            return await _mqttClient.ConnectAsync(_mqttClientOptions);
        }

        public async Task ListenToTopicAsync(string topic) {
            var mqttSubscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions);
        }

        public async Task DisconnectAsync() {
            await _mqttClient.DisconnectAsync();
        }

        public void Dispose() {
            _mqttClient?.Dispose();
        }
    }
}
