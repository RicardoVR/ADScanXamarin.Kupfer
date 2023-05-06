using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADScan.Client.Models
{
    public class FilterDevice
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Mac { get; set; }
        public string Name { get; set; }
    }
}
