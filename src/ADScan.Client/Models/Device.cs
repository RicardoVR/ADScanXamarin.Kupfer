using SQLite;
using System;
namespace ADScan.Client.Models
{
    public class Device
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Mac { get; set; }
        public string Number { get; set; }
        public string V1 { get; set; }
        public string V2 { get; set; }
        public string MM { get; set; }
        public int Desg { get; set; }
        public string RAW_ADV { get; set; }
        [Ignore]
        public string OriginalMac { get; set; }
    }
}

