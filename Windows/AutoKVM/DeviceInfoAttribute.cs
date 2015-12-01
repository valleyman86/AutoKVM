using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoKVM
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class DeviceInfoAttribute : System.Attribute
    {
        public ushort vendorID;
        public ushort productID;
    }
}
