using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADScan.Client.Models
{
    public class DataRequest
    {
        public DataRow[] Data { get; set; }
        public string Type { get; set; }
    }

    public class DataRow {
        public string Adc1 { get; set; }
        public string Adc2 { get; set; }
        public string Bate { get; set; }
        public int Cont { get; set; }
        public string Date { get; set; }
        public int Desg { get; set; }
        public string Hour { get; set; }
        public int Listened { get; set; }
        public string Mac { get; set; }
        public int Temp { get; set; }
        [JsonProperty("tipo")]
        public int Type { get; set; }
    }
}
