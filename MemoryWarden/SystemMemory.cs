using System.Runtime.InteropServices;

namespace MemoryWarden
{
    public class SystemMemory
    {
        //Much of this copy pasted from http://www.pinvoke.net/default.aspx/kernel32.globalmemorystatusex
        //Wish .NET had a simple way to use GlobalMemoryStatusEx()
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll")]//, CharSet = CharSet.Auto, EntryPoint = "GlobalMemoryStatusEx", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static double GetMemoryPercentUsed()
        {
            MEMORYSTATUSEX memoryInfo = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(memoryInfo);
            ulong freeBytes = memoryInfo.ullTotalPhys - memoryInfo.ullAvailPhys;
            double freeBytesPercent = (double)freeBytes / memoryInfo.ullTotalPhys * 100;
            return freeBytesPercent;
        }
    }
}
