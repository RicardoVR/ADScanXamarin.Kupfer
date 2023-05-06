using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ADScan.Client.Renderers
{
    public interface IFileAccessHelper
    {
       string GetLocalFilePath(string filename);
    }
}
