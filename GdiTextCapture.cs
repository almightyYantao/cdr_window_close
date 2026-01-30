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
            hMapFile = OpenFileMapping(FILE_MAP_READ, false, "Global\\CorelDrawGdiTextCapture");
            if (hMapFile == IntPtr.Zero)
            {
                return false;
            }

            pBuf = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 8192);
            if (pBuf == IntPtr.Zero)
            {
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
                return data.latestText;
            }
            catch
            {
                return null;
            }
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
