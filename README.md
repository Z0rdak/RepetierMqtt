# RepetierMqtt - A simple, easy configurable MQTT client to access your [Repetier Server](https://www.repetier-server.com/ "Repetier Server")

## Introduction

RepetierMqtt is a small and simple MQTT client which leverages the RepetierSharp client to forward information from the Repetier Server API to MQTT. 
It also provides the possibility to send commands via MQTT to the Repetier Server as well as upload and start gcode files.

### Framework support

Currently RepetierMqtt supports .NET Core 3.1 with plans to also support .NET 5 and above.

## Getting started

**DISCLAIMER:** *RepetierMqtt is still in beta. Bugs are to be expected - please bear with me and help improving it by submitting issues on [GitHub](https://github.com/Z0rdak/RepetierMqtt/issues).*

The following sections show some examples on how to use the RepetierMqtt client. The examples are not exhaustive. I will try to write a more thourough documentation as soon as possible.

RepetierMqtt uses a fluent builder api to reduce the complexity when creating a new client instance.

### Creating a MQTT client instance

The RepetierMqtt client needs an instance of `RepetierConnection` from RepetierSharp.

```csharp
RepetierConnection rc = new RepetierConnectionBuilder()
    .WithHost("demo.repetier-server.com", 4006)
    .WithApiKey("7075e377-7472-447a-b77e-86d481995e7b")
    .Build();
rc.Connect();

RepetierMqttClient MqttClient = new RepetierMqttClientBuilder()
    .WithRepetierConnection(rc)
    .WithMqttClientOptions(MqttOptionsProvider.DefaultMqttClientOptions)       
    .WithBaseTopic("RepetierMqtt/Neptune-19")
    .Build();

MqttClient.Connect();
```

This creates an simple instance with not much functionally other than offering you a MQTT client to publish messages. 
The provided `BaseTopic` is put in front of every other provided topic. The `MqttClientOption` type holds information about the broker, authentication, etc. See [MQTTnet](https://github.com/dotnet/MQTTnet/wiki/Client#client-options) for more information.

### Payload of published messages

In order to be able to identify the event and command responses, the published messages contain additional information. `data` contains the event payload or the command response payload.

#### Command responses example

```json
{
    "callbackId": 42,
    "command": "listModelGroups",
    "data" : {
      "groupNames": [
        "#",
        "My Robot"
      ],
        "ok": true
    }
}
```

#### Event payload example

```json
{
    "event": "jobFinished",
    "printer": "Cartesian",
    "data" : {
      "duration": 519,
      "end": 519,
      "lines": 18961,
      "start": 1615393295
    }
}
```

### Adding default topics for events and response messages

To forward events and responses from commands you can add default topics:

```csharp
RepetierMqttClient MqttClient = new RepetierMqttClientBuilder()
    .WithRepetierConnection(rc)
    .WithMqttClientOptions(MqttOptionsProvider.DefaultMqttClientOptions)       
    .WithBaseTopic("RepetierMqtt/Neptune-19")
    .WithDefaultEventTopic("Event")               // {BaseTopic}/Event[/{printer}]/{event}
    .WithDefaultResponseTopic("Response")         // {BaseTopic}/Response/{command}
    .Build();

MqttClient.Connect();
```

With this setup all events associated with a printer are published to their own topic `RepetierMqtt/Neptune-19/Event/{printer}/{eventName}`. 
Other events are published to `RepetierMqtt/Neptune-19/Event/{eventName}`.

Commands can be associated with a printer but are published to their own topic without the printer included `RepetierMqtt/Neptune-19/Response/{command}`.

### Defining topics for specific events or command responses

```csharp
RepetierMqttClient MqttClient = new RepetierMqttClientBuilder()
    .WithRepetierConnection(rc)
    .WithMqttClientOptions(MqttOptionsProvider.DefaultMqttClientOptions)       
    .WithBaseTopic("RepetierMqtt/Neptune-19")
    .WithCommandResponseTopic("startJob", "StartJobTopic")               //{BaseTopic}/StartJobTopic
    .WithPrinterEventTopic("temp", "Cartesian", "CartesianTempValues")   //{BaseTopic}/CartesianTempValues
    .WithEventTopic("temp", "AllTempValues")                             //{BaseTopic}/AllTempValues
    .Build();

MqttClient.Connect();
```
Line 5 adds a topic where only information about command responses for the command `startJob` is published: `RepetierMqtt/Neptune-19/StartJobTopic`.
The `callbackId` can be used to indentify the send command.

Line 6 adds a topic for temps event associated with the printer Cartesian: `RepetierMqtt/Neptune-19/CartesianTempValues`.
Line 7 adds a topic for all temp events:  `RepetierMqtt/Neptune-19/AllTempValues`.

###  Define topics for executing various repetier commands

```csharp
RepetierMqttClient MqttClient = new RepetierMqttClientBuilder()
    .WithRepetierConnection(rc)
    .WithMqttClientOptions(MqttOptionsProvider.DefaultMqttClientOptions)       
    .WithBaseTopic("RepetierMqtt/Neptune-19")
    .WithExecuteCommandTopic("Execute")           // {BaseTopic}/Execute          
    .Build();

MqttClient.Connect();
```

Different from the previous topics, this creates a subscription for the topic `RepetierMqtt/Neptune-19/Execute`. You are able to send any command to this topic to be executed. 

The payload expected needs some additional information beside the actual command:

```json
{
    "command": "startJob",
    "printer": "Cartesian",
    "data" : {
        "id": 6
    }
}
```

Where `data` is the actual object to execute the command. It is possible to leave the printer field blank if the command is not associated with a printer.

### Define topics with predefined commands

```csharp
RepetierMqttClient MqttClient = new RepetierMqttClientBuilder()
    .WithRepetierConnection(rc)
    .WithMqttClientOptions(MqttOptionsProvider.DefaultMqttClientOptions)       
    .WithBaseTopic("RepetierMqtt/Neptune-19")
    .WithPredefinedCommand("Action/ListPrinter", ListPrinterCommand.Instance)  // {BaseTopic}/Action/ListPrinter
    .WithPredefinedCommand("Model/Part42/EnqueueAndStart", new CopyModelCommand(42))  // {BaseTopic}/Model/Part42/EnqueueAndStart
    .Build();

MqttClient.Connect();
```

This (Line 5) creates a subscription for the topic `RepetierMqtt/Neptune-19/Action/ListPrinter` which executes a static, predefined command.
Line 6 creates a subscription for the topic `RepetierMqtt/Neptune-19/Model/Part42/EnqueueAndStart` which enqueues the specified model into the print queue and starts it when possible.

### Define topics to upload (and start) gcode files/jobs

```csharp
RepetierMqttClient MqttClient = new RepetierMqttClientBuilder()
    .WithRepetierConnection(rc)
    .WithMqttClientOptions(MqttOptionsProvider.DefaultMqttClientOptions)       
    .WithBaseTopic("RepetierMqtt/Neptune-19")
    .WithUploadGCodeTopic("UploadGCode")           // {BaseTopic}/UploadGCode
    .Build();

MqttClient.Connect();
```
This topic creates a subscription for the topic `RepetierMqtt/Neptune-19/UploadGCode`, which is used to upload gcode files and print them directly.

The payload expected for this topics looks like this: 

```json
{
    "filepath": "/path/to/file.gcode",
    "printer": "Cartesian",
    "autostart": false,
    "group": "#",
    "overwrite": true
}
```

Note the path for the gcode file needs to be accessible for the MQTT client. This uses the functions from RepetierSharp, which uses the REST-API to upload/start gcode files. 

If `autostart` is true, `group` and `overwrite` can be omitted. The gcode file is directly printed if possible or queued in the printing queue. 
If `autostart` is false, the gcode file is uploaded to the specified group of the printer. `overwrite` determines if a existing file with the same name should be replaced.
