using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;
using RepetierMqtt.Util;
using RepetierSharp;
using RepetierSharp.RepetierMqtt;
using RepetierSharp.RepetierMqtt.Util;

namespace RepetierSharp.RepetierMqtt
{
    public partial class RepetierMqttClient
    {

        public class RepetierMqttClientBuilder
        {

            private RepetierMqttClient _repetierMqttClient = new RepetierMqttClient();

            public RepetierMqttClientBuilder()
            {

            }

            public RepetierMqttClientBuilder WithRepetierConnection(RepetierConnection repetierConnection)
            {
                _repetierMqttClient.RepetierConnection = repetierConnection;
                return this;
            }

            public RepetierMqttClientBuilder WithMqttClientOptions(IMqttClientOptions options)
            {
                _repetierMqttClient.MqttClientOptions = options;
                return this;
            }

            public RepetierMqttClientBuilder WithBaseTopic(string baseTopic)
            {
                _repetierMqttClient.BaseTopic = baseTopic;
                return this;
            }

            // TODO: how to integrade printer and event info
            public RepetierMqttClientBuilder WithDefaultEventTopic(string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.DefaultEventTopic = BuildTopicFilter(topic, qos, retained);
                return this;
            }

            // TODO: how to integrade printer and command info
            public RepetierMqttClientBuilder WithDefaultResponseTopic(string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.DefaultResponseTopic = BuildTopicFilter(topic, qos, retained);
                return this;
            }

            // TODO: how to integrade printer info
            public RepetierMqttClientBuilder WithEventTopic(string eventName, string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.EventTopics.Add(eventName, BuildTopicFilter(topic, qos, retained));
                return this;
            }

            // TODO: how to integrade printer info
            public RepetierMqttClientBuilder WithCommandResponseTopic(string command, string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.CommandResponseTopics.Add(command, BuildTopicFilter(topic, qos, retained));
                return this;
            }

            public RepetierMqttClientBuilder WithReconnectDelay(uint delayInMs = 3000)
            {
                _repetierMqttClient.ReconnectDelay = delayInMs;
                return this;
            }

            public RepetierMqttClientBuilder WithUploadGCodeTopic(string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.UploadGCodeTopic = BuildTopicFilter(topic, qos, retained);
                return this;
            }

            public RepetierMqttClientBuilder WithExecuteCommandTopic(string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.ExecuteCommandTopic = BuildTopicFilter(topic, qos, retained);
                return this;
            }

            public RepetierMqttClient Build()
            {
                if (_repetierMqttClient.RepetierConnection == null)
                {
                    throw new ArgumentNullException("RepetierConnection needs to be provided");
                }

                if (_repetierMqttClient.MqttClientOptions == null)
                {
                    _repetierMqttClient.MqttClientOptions = MqttOptionsProvider.DefaultMqttClientOptions;
                }

                _repetierMqttClient.MqttClient = new MqttFactory().CreateMqttClient();

                _repetierMqttClient.MqttClient.UseConnectedHandler(async connectedArgs =>
                {
                    if (_repetierMqttClient.DefaultEventTopic != null)
                    {
                        await _repetierMqttClient.MqttClient.SubscribeAsync(_repetierMqttClient.DefaultEventTopic);
                    }

                    if (_repetierMqttClient.DefaultResponseTopic != null)
                    {
                        await _repetierMqttClient.MqttClient.SubscribeAsync(_repetierMqttClient.DefaultResponseTopic);
                    }

                    if (_repetierMqttClient.ExecuteCommandTopic != null)
                    {
                        await _repetierMqttClient.MqttClient.SubscribeAsync(_repetierMqttClient.ExecuteCommandTopic);
                    }

                    if (_repetierMqttClient.UploadGCodeTopic != null)
                    {
                        await _repetierMqttClient.MqttClient.SubscribeAsync(_repetierMqttClient.UploadGCodeTopic);
                    }

                    _repetierMqttClient.RepetierConnection.OnRawEvent += async (eventName, printer, eventData) =>
                    {
                        if (_repetierMqttClient.DefaultEventTopic != null)
                        {
                            var eventTopic = "";
                            if (!string.IsNullOrEmpty(printer))
                            {
                                eventTopic = $"{_repetierMqttClient.DefaultEventTopic.Topic}/{printer}/{eventName}";
                            }
                            else
                            {
                                eventTopic = $"{_repetierMqttClient.DefaultEventTopic.Topic}/{eventName}";
                            }
                            var topicFilter = new MqttTopicFilterBuilder()
                                .WithQualityOfServiceLevel(_repetierMqttClient.DefaultEventTopic.QualityOfServiceLevel)
                                .WithTopic(eventTopic)
                                .WithRetainAsPublished(_repetierMqttClient.DefaultEventTopic.RetainAsPublished)
                                .Build();
                            await _repetierMqttClient.MqttClient.PublishAsync(BuildMessage(topicFilter, eventData));
                        }
                        if (_repetierMqttClient.EventTopics.TryGetValue(eventName, out var topic))
                        {
                            await _repetierMqttClient.MqttClient.PublishAsync(topic.Topic, eventData);
                        }
                    };

                    _repetierMqttClient.RepetierConnection.OnRawResponse += async (callbackID, command, response) =>
                    {
                        if (_repetierMqttClient.DefaultResponseTopic != null)
                        {
                            var topicFilter = new MqttTopicFilterBuilder()
                                .WithQualityOfServiceLevel(_repetierMqttClient.DefaultResponseTopic.QualityOfServiceLevel)
                                .WithTopic($"{_repetierMqttClient.DefaultResponseTopic.Topic}/{command}")
                                .WithRetainAsPublished(_repetierMqttClient.DefaultResponseTopic.RetainAsPublished)
                                .Build();
                            await _repetierMqttClient.MqttClient.PublishAsync(BuildMessage(topicFilter, response));
                        }
                        if (_repetierMqttClient.CommandResponseTopics.TryGetValue(command, out var topic))
                        {
                            await _repetierMqttClient.MqttClient.PublishAsync(BuildMessage(topic, response));
                        }
                    };
                });

                _repetierMqttClient.MqttClient.UseDisconnectedHandler(async disconnectedArgs =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_repetierMqttClient.ReconnectDelay));
                    try
                    {
                        await _repetierMqttClient.Connect();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }
                });


                _repetierMqttClient.MqttClient.UseApplicationMessageReceivedHandler(e =>
                {
                    Console.WriteLine("Received");
                    var topic = e.ApplicationMessage.Topic;
                    if (_repetierMqttClient.ExecuteCommandTopic != null && topic == _repetierMqttClient.ExecuteCommandTopic.Topic)
                    {
                        try
                        {
                            // _repetierMqttClient.RepetierConnection.SendCommand(e.ApplicationMessage.Payload);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"{ex.Message}");
                        }
                    }

                    if (_repetierMqttClient.UploadGCodeTopic != null && topic == _repetierMqttClient.UploadGCodeTopic.Topic)
                    {
                        try
                        {
                            // TODO: validate payload
                            var gcodeCommand = JsonSerializer.Deserialize<UploadGCodeCommand>(e.ApplicationMessage.Payload);
                            if (gcodeCommand.Autostart)
                            {
                                _repetierMqttClient.RepetierConnection.UploadAndStartPrint(gcodeCommand.FilePath, gcodeCommand.Printer);
                            }
                            else
                            {
                                _repetierMqttClient.RepetierConnection.UploadGCode(gcodeCommand.FilePath, gcodeCommand.Group, gcodeCommand.Printer, gcodeCommand.Overwrite);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"{ex.Message}");
                        }
                    }

                });

                return _repetierMqttClient;
            }

            private MqttApplicationMessage BuildMessage(MqttTopicFilter topic, byte[] payload)
            {
                return new MqttApplicationMessageBuilder()
                    .WithTopic(topic.Topic)
                    .WithQualityOfServiceLevel(topic.QualityOfServiceLevel)
                    .WithRetainFlag(topic.RetainAsPublished)
                    .WithPayload(payload)
                    .Build();
            }

            private MqttTopicFilter BuildTopicFilter(string topic, int qos = 0, bool retained = false)
            {
                return new MqttTopicFilterBuilder()
                    .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                    .WithTopic(BuildTopic(topic))
                    .WithRetainAsPublished(retained)
                    .Build();
            }

            private string BuildTopic(string topic)
            {
                if (string.IsNullOrEmpty(_repetierMqttClient.BaseTopic))
                {
                    return topic;
                }
                else
                {
                    return $"{_repetierMqttClient.BaseTopic}/{topic}";
                }
            }
        }
    }
}
