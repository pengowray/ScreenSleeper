using MQTTnet;
using MQTTnet.Client;
using SleepScreenWPF.Settings;
using System;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MQTT {
    public class MQTTClient : IDisposable {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;
        public event EventHandler<string> StatusEvent;

        int RetryAttempts = 0;
        int MaxRetry = 5;
        bool KeepRetrying = true; // set to false e.g. when user disconnects or certificate fails

        public MQTTClient(SleepConfig config) {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            if (config == null) {
                throw new ArgumentException("Config is null");
            }

            _mqttClient.ApplicationMessageReceivedAsync += e => {
                MessageReceived?.Invoke(this, e);
                return Task.CompletedTask;
            };

            var mqttOptionsBuilder = new MqttClientOptionsBuilder();
            string protocol = config?.ParseProtocol() ?? "mqtt";
            if (protocol == "wss" || protocol == "ws") {
                //var wsUri = $"{config.Protocol}://{config.Server}:{config.ParsePort()}";
                var wsUri = $"{config?.Server}:{config?.ParsePort()}/{config?.SocketPath ?? ""}";
                //mqttOptionsBuilder = mqttOptionsBuilder.WithWebSocketServer(wsUri); // "obsolete"
                mqttOptionsBuilder = mqttOptionsBuilder.WithWebSocketServer(o => o.WithUri(wsUri));
            } else {
                mqttOptionsBuilder = mqttOptionsBuilder.WithTcpServer(config.Server, config.ParsePort());
            }

            if (protocol == "mqtts" || protocol == "wss") {
                mqttOptionsBuilder = mqttOptionsBuilder.WithTlsOptions(o => {
                    // The used public broker sometimes has invalid certificates. This sample accepts all
                    // certificates. This should not be used in live environments.
                    //o.CertificateValidationHandler = _ => true;
                    if ((config?.AcceptBadSSL ?? false) == true) {
                        o = o.WithCertificateValidationHandler(_ => true);
                        DisconnectAsync().Wait();
                    }

                    // The default value is determined by the OS. Set manually to force version.
                    //o.SslProtocol = SslProtocols.Tls12;
                    //o.WithSslProtocols(SslProtocols.Tls12);

                    // Please provide the file path of your certificate file.
                    //var certificate = new X509Certificate("/options/emqxsl-ca.crt", "");
                    //o.Certificates = new List<X509Certificate> { certificate };

                });
            }
            
            //mqttOptionsBuilder = mqttOptionsBuilder.WithClientId(config.);
            //mqttOptionsBuilder = mqttOptionsBuilder.WithAuthentication("password", Encoding.UTF8.GetBytes(password));
            mqttOptionsBuilder = mqttOptionsBuilder.WithCredentials(config.Username, config.Password);

            _mqttClientOptions = mqttOptionsBuilder.Build();

            //TODO: configurable reconnection
            _mqttClient.DisconnectedAsync += async (args) => {
                StatusEvent?.Invoke(this, $"### DISCONNECTED FROM SERVER ### {args.ConnectResult}: {args.ReasonString}");
                if (!KeepRetrying) {
                    return;
                } else if (++RetryAttempts > MaxRetry) {
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

            _mqttClient.ConnectedAsync += async (args) => {
                StatusEvent?.Invoke(this, $"### CONNECTED TO SERVER ### {args.ConnectResult.ResultCode} {args.ConnectResult.ResponseInformation}");
                RetryAttempts = 0;
                await Task.CompletedTask;
            };
        }

        public async Task<MqttClientConnectResult> ConnectAsync() {
            KeepRetrying = true;
            return await _mqttClient.ConnectAsync(_mqttClientOptions);
        }

        public async Task ListenToTopicAsync(string topic) {
            var mqttSubscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions);
        }

        public async Task DisconnectAsync() {
            KeepRetrying = false;
            if (_mqttClient != null) {
                await _mqttClient.DisconnectAsync();
            }
        }

        public void Dispose() {
            KeepRetrying = false;
            _mqttClient?.Dispose();
        }
    }
}
