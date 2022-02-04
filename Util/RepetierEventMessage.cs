using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace RepetierSharp.RepetierMqtt.Util
{
    public class RepetierEventMessage
    {
        [JsonPropertyName("event")]
        public string EventName { get; set; }

        [JsonPropertyName("printer")]
        public string Printer { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; }
    }
}
