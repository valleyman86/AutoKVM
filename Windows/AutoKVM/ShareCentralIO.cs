using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AutoKVM
{
    [DeviceInfo(vendorID = 0x4b4, productID = 0x120d)]
    class ShareCentralIO : IUSBDevice
    {
        IntPtr handle;

        public static ushort vendorID {
            get {
                DeviceInfoAttribute deviceInfoAttribute = (DeviceInfoAttribute)Attribute.GetCustomAttribute(typeof(ShareCentralIO), typeof(DeviceInfoAttribute));
                return deviceInfoAttribute.vendorID;
            }
        }
        public static ushort productID {
            get {
                DeviceInfoAttribute deviceInfoAttribute = (DeviceInfoAttribute)Attribute.GetCustomAttribute(typeof(ShareCentralIO), typeof(DeviceInfoAttribute));
                return deviceInfoAttribute.productID;
            }
        }

        public int numberOfPorts { get { return 2; } }

        public enum Devices {
            Device4 = 0x01,
            Device3 = 0x02,
            Device2 = 0x04,
            Device1 = 0x08
        }

        public ShareCentralIO()
        {
            handle = HIDAPI.HIDOpen(vendorID, productID, null);
            if(handle == null) {
                Console.WriteLine("Unable to open USB device.\n");
                throw new FailedToOpenDeviceException("Unable to open ShareCentral USB device.");
            }
        }

        ~ShareCentralIO()
        {
            Close();
        }

        public Devices SwitchAll()
        {
            return SwitchDevices(Devices.Device1 | Devices.Device2 | Devices.Device3 | Devices.Device4);
        }

        public Devices SwitchDevices(Devices device)
        {
            byte[] data = new byte[8];
            data[0] = 0x02;
            data[1] = 0x55;
            data[2] = (byte)device;

            int result = HIDAPI.HIDWrite(handle, data);

            //TODO: This fails to read if the write fails. It blocks forever. 
            data = HIDAPI.HIDRead(handle);

            if(data[1] == 0x5c)
                return (Devices)data[2];
            else {
                Console.WriteLine("Unable to read switch status.\n");
                return 0;
            }
        }

        public Devices GetStatusOfDevices()
        {
            byte[] data = new byte[8];
            data[0] = 0x02;
            data[1] = 0x5b;
            data[2] = 0x0f;
            HIDAPI.HIDWrite(handle, data);

            data = HIDAPI.HIDRead(handle);

            if(data[1] == 0x5c)
                return (Devices)data[2];
            else {
                Console.WriteLine("Unable to read switch status.\n");
                return 0;
            }
        }

        public void CyclePorts(int[] enabledPorts)
        {
            ShareCentralIO.Devices status = GetStatusOfDevices();
            bool device1Status = (status & ShareCentralIO.Devices.Device1) == ShareCentralIO.Devices.Device1;
            bool device2Status = (status & ShareCentralIO.Devices.Device2) == ShareCentralIO.Devices.Device2;

            if (device1Status == device2Status) {
                SwitchDevices(ShareCentralIO.Devices.Device1);
                SwitchDevices(ShareCentralIO.Devices.Device2);
            } else if (device1Status == true && device2Status == false) {
                SwitchDevices(ShareCentralIO.Devices.Device1);
            } else if (device1Status == false && device2Status == true) {
                SwitchDevices(ShareCentralIO.Devices.Device2);
            }
        }

        public void Close()
        {
            if(handle != IntPtr.Zero) {
                HIDAPI.HIDClose(handle);
                HIDAPI.HIDExit();
                handle = IntPtr.Zero;
            }
        }

    }
}
