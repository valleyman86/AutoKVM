using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AutoKVM
{
    class HIDAPI
    {
        [StructLayout(LayoutKind.Sequential)]
        public class OVERLAPPED
        {
            public UIntPtr Internal;
            public UIntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr EventHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class HIDDevice
        {
            public IntPtr device_handle;
            public bool blocking;
            public uint input_report_length;
            public IntPtr last_error_str;
            public int last_error_num;
            public bool read_pending;
            public string read_buf;
            public OVERLAPPED ol;
        }

        [DllImport(@"hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr hid_open(ushort vendor_id, ushort product_id, string serial_number);

        public static IntPtr HIDOpen(ushort vendor_id, ushort product_id, string serial_number)
        {
            IntPtr handle = hid_open(vendor_id, product_id, serial_number);

            return handle;
        }

        [DllImport(@"hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void hid_close(IntPtr dev);

        public static void HIDClose(IntPtr dev)
        {
            hid_close(dev);
        }

        [DllImport(@"hidapi.dll", EntryPoint = @"hid_exit", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HIDExit();

        [DllImport(@"hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int hid_write(IntPtr dev, byte[] data, uint length);

        public static int HIDWrite(IntPtr dev, byte[] data)
        {
            int result = hid_write(dev, data, (uint)data.Length);

            return result;
        }

        [DllImport(@"hidapi.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int hid_read(IntPtr dev, IntPtr data, uint length);

        public static byte[] HIDRead(IntPtr dev)
        {
            byte[] data = new byte[8];
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(data[0]) * data.Length);

            int result = hid_read(dev, p, (uint)data.Length);

            byte[] returnData = new byte[result];
            Marshal.Copy(p, returnData, 0, result);

            Marshal.FreeHGlobal(p);

            return returnData;
        }
    }
}
