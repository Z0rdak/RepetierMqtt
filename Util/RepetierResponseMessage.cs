using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepetierSharp.RepetierMqtt.Util
{
    public class RepetierResponseMessage
    {

        [JsonPropertyName("callbackId")]
        public int CallbackId { get; set; }

        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; }
    }
}
