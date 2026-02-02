using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CorelDrawAutoIgnoreError
{
    public class GdiTextCapture
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        const uint FILE_MAP_READ = 0x0004;
        const uint FILE_MAP_WRITE = 0x0002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SharedTextData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4000)]
            public string latestText;
            public uint processId;
            public uint timestamp;
        }

        private IntPtr hMapFile = IntPtr.Zero;
        private IntPtr pBuf = IntPtr.Zero;

        public bool Initialize()
        {
            hMapFile = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, false, "Global\\CorelDrawGdiTextCapture");
            if (hMapFile == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                    $"[{DateTime.Now:HH:mm:ss}] 共享内存打开失败, 错误代码: {error}\n");
                return false;
            }

            pBuf = MapViewOfFile(hMapFile, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 8192);
            if (pBuf == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                    $"[{DateTime.Now:HH:mm:ss}] 共享内存映射失败, 错误代码: {error}\n");
                CloseHandle(hMapFile);
                hMapFile = IntPtr.Zero;
                return false;
            }

            return true;
        }

        public string GetLatestText()
        {
            if (pBuf == IntPtr.Zero) return null;

            try
            {
                SharedTextData data = Marshal.PtrToStructure<SharedTextData>(pBuf);

                // 调试：记录共享内存中的所有信息
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log"),
                    $"[{DateTime.Now:HH:mm:ss}]     [GDI共享内存] Text=[{data.latestText}], PID={data.processId}, Time={data.timestamp}\n");

                return data.latestText;
            }
            catch
            {
                return null;
            }
        }

        // 清空共享内存中的文本（用于处理完一个对话框后）
        public void ClearText()
        {
            if (pBuf == IntPtr.Zero) return;

            try
            {
                SharedTextData data = new SharedTextData();
                data.latestText = "";
                data.processId = 0;
                data.timestamp = 0;
                Marshal.StructureToPtr(data, pBuf, false);
            }
            catch { }
        }

        public void Cleanup()
        {
            if (pBuf != IntPtr.Zero)
            {
                UnmapViewOfFile(pBuf);
                pBuf = IntPtr.Zero;
            }

            if (hMapFile != IntPtr.Zero)
            {
                CloseHandle(hMapFile);
                hMapFile = IntPtr.Zero;
            }
        }
    }
}
