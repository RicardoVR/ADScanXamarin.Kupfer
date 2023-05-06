using System;
using SQLite;

namespace ADScan.Client.Models
{
    public class DeviceConfiguration
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Index { get; set; }
        public string Value { get; set; }
        public bool Enabled { get; set; }
    }
}

