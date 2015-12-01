using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoKVM
{
    [Serializable()]
    public class FailedToOpenDeviceException : System.Exception
    {
        public FailedToOpenDeviceException() : base() { }
        public FailedToOpenDeviceException(string message) : base(message) { }
        public FailedToOpenDeviceException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected FailedToOpenDeviceException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
