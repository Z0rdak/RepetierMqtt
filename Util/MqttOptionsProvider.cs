using MQTTnet.Client;
using System;

namespace RepetierSharp.RepetierMqtt.Util
{
    public class MqttOptionsProvider
    {
        public static MqttClientOptions DefaultMqttClientOptions
        {
            get { return _mqttClientOptions ?? DefaultClientOptions(); }
            set { _mqttClientOptions = value ?? DefaultClientOptions(); }
        }

        private static MqttClientOptions _mqttClientOptions;

        public static string DefaultBrokerUrl
        {
            get { return _brokerUrl ?? "broker.hivemq.com"; }
            set { _brokerUrl = value ?? "broker.hivemq.com"; }
        }

        private static string _brokerUrl;

        private static MqttClientOptions DefaultClientOptions(string clientId = null)
        {
            return new MqttClientOptionsBuilder()
                .WithClientId(clientId ?? $"{Guid.NewGuid()}")
                .WithTcpServer(DefaultBrokerUrl)
                .Build();
        }
    }

}
