using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADScan.Client.Models
{
    public class DeviceMessage
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Raw { get; set; }
        public string MacAddress { get; set; }
        public string Device { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
