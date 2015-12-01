using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoKVM
{
    interface IUSBDevice
    {
        int numberOfPorts { get; }

        void CyclePorts(int[] enabledPorts);
    }
}
