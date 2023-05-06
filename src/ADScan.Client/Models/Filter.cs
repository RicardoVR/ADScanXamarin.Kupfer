using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace ADScan.Client.Models
{
    public class Filter
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string Device { get; set; }
    }
}
