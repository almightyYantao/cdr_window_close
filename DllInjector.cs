using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace CorelDrawAutoIgnoreError
{
    public class DllInjector
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 0x04;

        public static bool InjectDll(int processId, string dllPath)
        {
            try
            {
                // 打开目标进程
                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                if (hProcess == IntPtr.Zero)
                {
                    Debug.WriteLine($"无法打开进程 {processId}");
                    return false;
                }

                // 在目标进程中分配内存
                byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath);
                IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPathBytes.Length, MEM_COMMIT, PAGE_READWRITE);
                if (allocMem == IntPtr.Zero)
                {
                    Debug.WriteLine("内存分配失败");
                    CloseHandle(hProcess);
                    return false;
                }

                // 写入DLL路径
                IntPtr bytesWritten;
                if (!WriteProcessMemory(hProcess, allocMem, dllPathBytes, (uint)dllPathBytes.Length, out bytesWritten))
                {
                    Debug.WriteLine("写入内存失败");
                    CloseHandle(hProcess);
                    return false;
                }

                // 获取LoadLibraryW地址
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    Debug.WriteLine("获取LoadLibraryW地址失败");
                    CloseHandle(hProcess);
                    return false;
                }

                // 创建远程线程加载DLL
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMem, 0, IntPtr.Zero);
                if (hThread == IntPtr.Zero)
                {
                    Debug.WriteLine("创建远程线程失败");
                    CloseHandle(hProcess);
                    return false;
                }

                CloseHandle(hThread);
                CloseHandle(hProcess);

                Debug.WriteLine($"成功注入DLL到进程 {processId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"注入失败: {ex.Message}");
                return false;
            }
        }

        public static int FindCorelDrawProcess()
        {
            var processes = Process.GetProcessesByName("CorelDRW");
            if (processes.Length > 0)
            {
                return processes[0].Id;
            }
            return -1;
        }
    }
}
