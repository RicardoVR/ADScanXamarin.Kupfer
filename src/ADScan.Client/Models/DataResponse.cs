using System;
using System.Collections.Generic;
using System.Text;

namespace ADScan.Client.Models
{
    public class DataResponse
    {
        public int Type { get; set; }
        public string MessageTitle { get; set; }
        public string MessageInfo { get; set; }
        public string MessageAdditional { get; set; }
        public int NumEntities { get; set; }
        public string Json { get; set; }
    }
}
