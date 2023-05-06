using System;
using SQLite;

namespace ADScan.Client.Models
{
    public class MassiveDevice
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
    }
}

