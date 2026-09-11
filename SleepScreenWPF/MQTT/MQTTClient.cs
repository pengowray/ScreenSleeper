using MQTTnet;
using SleepScreenWPF.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTT {
    public class MQTTClient : IDisposable {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;
        public event EventHandler<string> StatusEvent;

        int RetryAttempts = 0;
        bool KeepRetrying = true; // set to false e.g. when user disconnects or certificate fails
        CancellationTokenSource RetryCancel = new CancellationTokenSource(); // cancels the wait between retries

        // Topics we have asked for, so we can ask again after a reconnect.
        readonly HashSet<string> SubscribedTopics = new HashSet<string>();
        readonly object TopicsLock = new object(); // added from the UI thread, read from MQTTnet's

        public MQTTClient(SleepConfig config) {
            var mqttClientFactory = new MqttClientFactory();
            _mqttClient = mqttClientFactory.CreateMqttClient();

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
                // MQTTnet 4 took a schemeless host:port/path here and picked ws or wss from the TLS
                // options. MQTTnet 5 rejects anything without a ws:// or wss:// scheme, so pass it.
                string socketPath = config?.SocketPath ?? "";
                if (!socketPath.StartsWith("/")) {
                    socketPath = "/" + socketPath;
                }
                var wsUri = $"{protocol}://{config?.Server}:{config?.ParsePort()}{socketPath}";
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
                    if ((config?.AllowBadSSL ?? false) == true) {
                        o.WithCertificateValidationHandler(_ => true);
                    }

                    // The default value is determined by the OS. Set manually to force version.
                    //o.SslProtocol = SslProtocols.Tls12;
                    //o.WithSslProtocols(SslProtocols.Tls12);

                    //TODO: allow user certs?
                    // Please provide the file path of your certificate file.
                    //var certificate = new X509Certificate("/options/emqxsl-ca.crt", "");
                    //o.Certificates = new List<X509Certificate> { certificate };

                });
            }
            
            //mqttOptionsBuilder = mqttOptionsBuilder.WithClientId(config.);
            //mqttOptionsBuilder = mqttOptionsBuilder.WithAuthentication("password", Encoding.UTF8.GetBytes(password));
            mqttOptionsBuilder = mqttOptionsBuilder.WithCredentials(config.Username, config.Password);

            _mqttClientOptions = mqttOptionsBuilder.Build();

            _mqttClient.DisconnectedAsync += async (args) => {
                StatusEvent?.Invoke(this, $"### DISCONNECTED FROM SERVER ### {args.ConnectResult}: {args.ReasonString}");
                if (!KeepRetrying) {
                    return;
                }

                int maxRetry = config.ParseMaxRetry(); // negative means retry indefinitely
                RetryAttempts++;
                if (maxRetry >= 0 && RetryAttempts > maxRetry) {
                    StatusEvent?.Invoke(this, maxRetry == 0
                        ? "### NOT RECONNECTING: RETRIES ARE TURNED OFF ###"
                        : "### MAX RETRY ATTEMPTS REACHED ###");
                    return;
                }

                var delay = RetryDelay(RetryAttempts);
                string attempt = maxRetry > 0 ? $"{RetryAttempts}/{maxRetry}" : $"{RetryAttempts}";
                StatusEvent?.Invoke(this, $"### RECONNECTING {attempt} IN {delay.TotalSeconds:0}s ###");

                try {
                    await Task.Delay(delay, RetryCancel.Token);
                } catch (OperationCanceledException) {
                    return; // user disconnected while we were waiting
                }
                if (!KeepRetrying) {
                    return;
                }

                try {
                    await _mqttClient.ConnectAsync(_mqttClientOptions);
                } catch {
                    // a failed connect raises DisconnectedAsync again, which schedules the next attempt
                    StatusEvent?.Invoke(this, "### RECONNECTION ERROR ###");
                }
            };

            _mqttClient.ConnectedAsync += async (args) => {
                StatusEvent?.Invoke(this, $"### CONNECTED TO SERVER ### {args.ConnectResult.ResultCode} {args.ConnectResult.ResponseInformation}");
                if (args.ConnectResult.ResultCode == MqttClientConnectResultCode.Success) {
                    RetryAttempts = 0;
                    await ResubscribeAsync();
                }

                if (!string.IsNullOrWhiteSpace(args.ConnectResult.ServerReference)) {
                    //TODO: automatically connect to server reference?
                    //ServerReference = args.ConnectResult.ServerReference;
                    StatusEvent?.Invoke(this, $"### SERVER REFERENCE (use instead?): {args.ConnectResult.ServerReference}");
                }

                if (!string.IsNullOrWhiteSpace(args.ConnectResult.AssignedClientIdentifier)) {
                    StatusEvent?.Invoke(this, $"### Assigned Client Identifier: {args.ConnectResult.AssignedClientIdentifier}");
                }

                //should check this in case user has used wildcards and not available
                //WildcardSubscriptionAvailable = args.ConnectResult.WildcardSubscriptionAvailable;

                await Task.CompletedTask;
            };
        }

        // Wait between reconnection attempts: 5s, 10s, 20s, 40s, then 60s for every attempt after that.
        private static TimeSpan RetryDelay(int attempt) {
            const int baseSeconds = 5;
            const int maxSeconds = 60;
            int shift = Math.Min(Math.Max(attempt - 1, 0), 5);
            return TimeSpan.FromSeconds(Math.Min(baseSeconds << shift, maxSeconds));
        }

        public async Task<MqttClientConnectResult> ConnectAsync() {
            KeepRetrying = true;
            RetryAttempts = 0;
            if (RetryCancel.IsCancellationRequested) {
                RetryCancel = new CancellationTokenSource();
            }
            return await _mqttClient.ConnectAsync(_mqttClientOptions);
        }

        public async Task ListenToTopicAsync(string topic) {
            lock (TopicsLock) {
                SubscribedTopics.Add(topic);
            }

            var mqttSubscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions);
        }

        // We connect with a clean session, so the broker forgets our subscriptions every time the
        // connection drops. Ask for them again once we are back.
        private async Task ResubscribeAsync() {
            string[] topics;
            lock (TopicsLock) {
                topics = SubscribedTopics.ToArray();
            }

            if (topics.Length == 0) {
                return; // first connect: the caller subscribes once ConnectAsync returns
            }

            var builder = new MqttClientSubscribeOptionsBuilder();
            foreach (var topic in topics) {
                builder = builder.WithTopicFilter(f => f.WithTopic(topic));
            }

            try {
                await _mqttClient.SubscribeAsync(builder.Build());
                StatusEvent?.Invoke(this, $"### LISTENING AGAIN TO {topics.Length} TOPIC(S) ###");
            } catch (Exception ex) {
                StatusEvent?.Invoke(this, $"### COULD NOT LISTEN AGAIN ### {ex.Message}");
            }
        }

        public async Task DisconnectAsync() {
            KeepRetrying = false;
            RetryCancel.Cancel(); // stop waiting out a backoff delay
            if (_mqttClient != null) {
                await _mqttClient.DisconnectAsync();
            }
        }

        public void Dispose() {
            KeepRetrying = false;
            RetryCancel.Cancel();
            _mqttClient?.Dispose();
        }
    }
}
