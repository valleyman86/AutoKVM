using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AutoKVM
{
    class ShareCentralIO
    {
        IntPtr handle;

        public enum Devices {
            Device3 = 0x01,
            Device4 = 0x02,
            Device1 = 0x04,
            Device2 = 0x08
        }

        public ShareCentralIO()
        {
            handle = HIDAPI.HIDOpen(0x4b4, 0x120d, null);
            if(handle == null) {
                Console.WriteLine("Unable to open USB device.\n");
                return;
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

        public Devices SwitchDevices(Devices devices)
        {
            byte[] data = new byte[8];
            data[0] = 0x02;
            data[1] = 0x55;
            data[2] = (byte)devices;

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
