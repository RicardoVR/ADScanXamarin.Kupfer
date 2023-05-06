using System;
using System.Collections.Generic;
using System.Text;

namespace ADScan.Client.Renderers
{
    public interface IBackgroundService
    {
        void Start();

        void Stop();

        bool IsRunning(string name);
    }
}
