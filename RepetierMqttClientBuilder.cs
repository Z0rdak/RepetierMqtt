﻿using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using RepetierSharp.Models.Commands;
using RepetierSharp.RepetierMqtt.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

            public RepetierMqttClientBuilder WithMqttClientOptions(MqttClientOptions options)
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

            public RepetierMqttClientBuilder WithDefaultResponseTopic(string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.DefaultResponseTopic = BuildTopicFilter(topic, qos, retained);
                return this;
            }

            public RepetierMqttClientBuilder WithEventTopic(string eventName, string topic, int qos = 0, bool retained = false)
            {
                _repetierMqttClient.EventTopics.Add(eventName, BuildTopicFilter(topic, qos, retained));
                return this;
            }

            public RepetierMqttClientBuilder WithPrinterEventTopic(string eventName, string printer, string topic, int qos = 0, bool retained = false)
            {
                if (!_repetierMqttClient.PrinterEventTopics.ContainsKey(printer))
                {
                    _repetierMqttClient.PrinterEventTopics.Add(printer, new Dictionary<string, MqttTopicFilter>());
                }
                _repetierMqttClient.PrinterEventTopics[printer].Add(eventName, BuildTopicFilter(topic, qos, retained));
                return this;
            }


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

            public RepetierMqttClientBuilder WithPredefinedCommand(string topic, ICommandData command)
            {
                _repetierMqttClient.CommandMapping.Add(topic, command);
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

                _repetierMqttClient.MqttClient.ConnectedAsync += (async connectedArgs =>
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

                    foreach (var entry in _repetierMqttClient.CommandMapping)
                    {
                        await _repetierMqttClient.MqttClient.SubscribeAsync(entry.Key);
                    }

                    // forward/publish event messages
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

                            var RepetierEventMsg = new RepetierEventMessage
                            {
                                EventName = eventName,
                                Printer = printer
                            };
                            try
                            {
                                RepetierEventMsg.Data = JsonSerializer.Deserialize<Dictionary<string, object>>(eventData);
                            }
                            catch (Exception ex)
                            {
                                RepetierEventMsg.Data = new Dictionary<string, object>();
                            }

                            var topicFilter = new MqttTopicFilterBuilder()
                                .WithQualityOfServiceLevel(_repetierMqttClient.DefaultEventTopic.QualityOfServiceLevel)
                                .WithTopic(eventTopic)
                                .WithRetainAsPublished(_repetierMqttClient.DefaultEventTopic.RetainAsPublished)
                                .Build();
                            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(RepetierEventMsg));
                            await _repetierMqttClient.MqttClient.PublishAsync(BuildMessage(topicFilter, payload));
                        }
                        if (_repetierMqttClient.EventTopics.TryGetValue(eventName, out var topic))
                        {
                            var mqttMsg = new MqttApplicationMessageBuilder()
                            .WithTopic(topic.Topic)
                            .WithPayload(eventData)
                            .Build();
                            await _repetierMqttClient.MqttClient.PublishAsync(mqttMsg);
                        }

                        if (!string.IsNullOrEmpty(printer))
                        {
                            if (_repetierMqttClient.PrinterEventTopics.TryGetValue(printer, out var eventTopicMap))
                            {
                                if (eventTopicMap.TryGetValue(eventName, out var topic1))
                                {
                                    var mqttMsg = new MqttApplicationMessageBuilder()
                                    .WithTopic(topic1.Topic)
                                    .WithPayload(eventData)
                                    .Build();
                                    await _repetierMqttClient.MqttClient.PublishAsync(mqttMsg);
                                }
                            }
                        }
                    };

                    // forward/publish response messages
                    _repetierMqttClient.RepetierConnection.OnRawResponse += async (callbackID, command, response) =>
                    {
                        if (_repetierMqttClient.DefaultResponseTopic != null)
                        {
                            var RepetierResponseMSg = new RepetierResponseMessage
                            {
                                CallbackId = callbackID,
                                Command = command
                            };
                            try
                            {
                                RepetierResponseMSg.Data = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                            }
                            catch (Exception ex)
                            {
                                RepetierResponseMSg.Data = new Dictionary<string, object>();
                            }
                            var topicFilter = new MqttTopicFilterBuilder()
                                .WithQualityOfServiceLevel(_repetierMqttClient.DefaultResponseTopic.QualityOfServiceLevel)
                                .WithTopic($"{_repetierMqttClient.DefaultResponseTopic.Topic}/{command}")
                                .WithRetainAsPublished(_repetierMqttClient.DefaultResponseTopic.RetainAsPublished)
                                .Build();
                            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(RepetierResponseMSg));

                            await _repetierMqttClient.MqttClient.PublishAsync(BuildMessage(topicFilter, payload));
                        }
                        if (_repetierMqttClient.CommandResponseTopics.TryGetValue(command, out var topic))
                        {
                            var topicFilter = new MqttTopicFilterBuilder()
                               .WithQualityOfServiceLevel(topic.QualityOfServiceLevel)
                               .WithTopic(topic.Topic)
                               .WithRetainAsPublished(topic.RetainAsPublished)
                               .Build();
                            await _repetierMqttClient.MqttClient.PublishAsync(BuildMessage(topicFilter, response));
                        }
                    };
                });

                _repetierMqttClient.MqttClient.DisconnectedAsync += (async disconnectedArgs =>
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


                _repetierMqttClient.MqttClient.ApplicationMessageReceivedAsync += async e =>
                {
                    var topic = e.ApplicationMessage.Topic;
                    if (_repetierMqttClient.ExecuteCommandTopic != null && topic == _repetierMqttClient.ExecuteCommandTopic.Topic)
                    {
                        try
                        {
                            // TODO: validate payload and handle error
                            var rawCommand = JsonSerializer.Deserialize<RawCommand>(e.ApplicationMessage.Payload);
                            _repetierMqttClient.RepetierConnection.SendCommand(rawCommand.Command, rawCommand.Printer, rawCommand.Data);
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
                            // TODO: validate payload and handle error
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

                    foreach (var entry in _repetierMqttClient.CommandMapping)
                    {
                        if (entry.Key == topic)
                        {
                            await _repetierMqttClient.RepetierConnection.SendCommand(entry.Value);
                        }
                    }
                };

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
