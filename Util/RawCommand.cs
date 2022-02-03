using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepetierSharp.RepetierMqtt.Util
{
    public class RawCommand
    {
        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("printer")]
        public string Printer { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; }
    }
}
