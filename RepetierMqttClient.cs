using MQTTnet.Client;
using MQTTnet.Packets;
using RepetierSharp.Models.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepetierSharp.RepetierMqtt
{
    public partial class RepetierMqttClient
    {
        /// <summary>
        /// Mapping for events -> topics
        /// Used to determine where to publish the data of an event
        /// </summary>
        private Dictionary<string, MqttTopicFilter> EventTopics { get; set; } = new Dictionary<string, MqttTopicFilter>();

        /// <summary>
        /// Mapping for events -> topics
        /// Used to determine where to publish the data of an event
        /// </summary>
        private Dictionary<string, Dictionary<string, MqttTopicFilter>> PrinterEventTopics { get; set; } = new Dictionary<string, Dictionary<string, MqttTopicFilter>>();


        /// <summary>
        /// Mapping for command -> topics
        /// Used to determine where to publish the response data of a sent command
        /// </summary>
        private Dictionary<string, MqttTopicFilter> CommandResponseTopics { get; set; } = new Dictionary<string, MqttTopicFilter>();

        public RepetierConnection RepetierConnection { get; private set; }

        public string BaseTopic { get; set; }

        private IMqttClient MqttClient { get; set; }

        private MqttClientOptions MqttClientOptions { get; set; }

        private uint ReconnectDelay { get; set; } = 3000;

        /// <summary>
        /// Topic -> Command to execute 
        /// </summary>
        private Dictionary<string, ICommandData> CommandMapping { get; set; } = new Dictionary<string, ICommandData>();

        /// <summary>
        /// Topic for starting / uploading gcode.
        /// Expected payload: UploadGCodeCommand
        /// </summary>
        public MqttTopicFilter UploadGCodeTopic { get; private set; }

        /// <summary>
        /// Topic for executing repetier commands.
        /// Expected payload depending on command.
        /// </summary>
        public MqttTopicFilter ExecuteCommandTopic { get; private set; }

        public MqttTopicFilter DefaultResponseTopic { get; private set; }

        public MqttTopicFilter DefaultEventTopic { get; private set; }

        private RepetierMqttClient() { }

        public Task<MqttClientConnectResult> Connect()
        {
            return MqttClient.ConnectAsync(MqttClientOptions);
        }

        public Task Disconnect()
        {
            return MqttClient.DisconnectAsync();
        }
    }
}
