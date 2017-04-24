using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MemoryWarden
{
    class SharedStatics
    {
        public static Brush CalculateMemoryBrush(double memoryPercent)
        {
            //Calcualte colors
            double memoryGoodRatio;
            // Used for calculating the green/red color
            // 60% or less is good, 90% or more is bad
            if (memoryPercent <= 60) memoryGoodRatio = 1;
            else if (memoryPercent >= 90) memoryGoodRatio = 0;
            else memoryGoodRatio = (90 - memoryPercent) / 30;

            double green = memoryGoodRatio * 0xFF;
            double red = (1 - memoryGoodRatio) * 0xFF;
            return new SolidColorBrush(Color.FromArgb(0xFF, (byte)red, (byte)green, 0));
        }

        public static ImageSource ToImageSource(System.Drawing.Icon icon)
        {
            //Used when converting a .ico resource to a window icon.
            ImageSource imageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            return imageSource;
        }
        
        //Imports the FlashWindowEx function
        //Mostly copied from http://stackoverflow.com/questions/73162/how-to-make-the-taskbar-blink-my-application-like-messenger-does-when-a-new-mess

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        public const UInt32 FLASHW_ALL = 0x00000003;
        //Flash both the window caption and taskbar button.This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        public const UInt32 FLASHW_CAPTION = 0x00000001;
        //Flash the window caption.
        public const UInt32 FLASHW_STOP = 0;
        //Stop flashing. The system restores the window to its original state.
        public const UInt32 FLASHW_TIMER = 0x00000004;
        //Flash continuously, until the FLASHW_STOP flag is set.
        public const UInt32 FLASHW_TIMERNOFG = 0x0000000C;
        //Flash continuously until the window comes to the foreground.
        public const UInt32 FLASHW_TRAY = 0x00000002;
        //Flash the taskbar

        public static bool FlashWindow(Window window, UInt32 flags, UInt32 flashRate = 0, UInt32 flashCount = UInt32.MaxValue)
        {
            //See https://msdn.microsoft.com/en-us/library/windows/desktop/ms679347(v=vs.85).aspx
            // for details on the parameters. 
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = new WindowInteropHelper(window).EnsureHandle();
            fInfo.dwFlags = flags;
            fInfo.uCount = flashCount;
            fInfo.dwTimeout = flashRate;

            return FlashWindowEx(ref fInfo);
        }
    }
}
