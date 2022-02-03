﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace RepetierMqtt.Util
{
    public class UploadGCodeCommand
    {
        [JsonPropertyName("filepath")]
        public string FilePath { get; set; }

        [JsonPropertyName("printer")]
        public string Printer { get; set; }

        [JsonPropertyName("autostart")]
        public bool Autostart { get; set; }

        [JsonPropertyName("group")]
        public string Group { get; set; }

        [JsonPropertyName("overwrite")]
        public bool Overwrite { get; set; }
    }
}
