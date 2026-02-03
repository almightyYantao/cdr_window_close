using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;

// 单文件版本 - CorelDRAW按钮检测工具
// 使用方法: csc /target:exe /out:ButtonDetector.exe ButtonDetectorStandalone.cs
//          或者直接运行 COMPILE.bat

namespace CorelDrawButtonDetector
{
    class Program
    {
        // Windows API
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        const uint WM_GETTEXT = 0x000D;
        static string logPath;

        static void Main(string[] args)
        {
            logPath = Path.Combine(Environment.CurrentDirectory, "button_detection.log");
            File.WriteAllText(logPath, "=== CorelDRAW 按钮检测日志 " + DateTime.Now + " ===\n\n");

            Console.WriteLine("=== CorelDRAW 按钮检测工具 ===");
            Console.WriteLine("日志文件: " + logPath);
            Console.WriteLine("\n请确保CorelDRAW已打开并有错误对话框");
            Console.WriteLine("\n按任意键开始扫描...");
            Console.ReadKey();

            Console.WriteLine("\n开始扫描...\n");
            ScanAllWindows();

            Console.WriteLine("\n扫描完成！");
            Console.WriteLine("详细信息请查看: " + logPath);
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static void ScanAllWindows()
        {
            Log("===== 开始扫描所有窗口 =====\n");

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder titleSb = new StringBuilder(256);
                GetWindowText(hWnd, titleSb, titleSb.Capacity);
                string title = titleSb.ToString();

                if (string.IsNullOrWhiteSpace(title)) return true;

                // 扫描所有窗口，不仅限于 CorelDRAW
                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                Log("\n【窗口】 " + title);
                Log("  进程ID: " + processId);
                Log("  句柄: 0x" + hWnd.ToInt64().ToString("X") + "\n");

                ScanWindowControls(hWnd);

                return true;
            }, IntPtr.Zero);

            Log("\n===== 扫描完成 =====");
        }

        static void ScanWindowControls(IntPtr hwnd)
        {
            int controlCount = 0;
            int buttonCount = 0;
            var buttonList = new System.Collections.Generic.List<string>();

            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                controlCount++;

                StringBuilder classSb = new StringBuilder(256);
                GetClassName(childHwnd, classSb, classSb.Capacity);
                string className = classSb.ToString();

                StringBuilder textSb1 = new StringBuilder(512);
                GetWindowText(childHwnd, textSb1, textSb1.Capacity);
                string text1 = textSb1.ToString();

                StringBuilder textSb2 = new StringBuilder(512);
                SendMessage(childHwnd, WM_GETTEXT, (IntPtr)textSb2.Capacity, textSb2);
                string text2 = textSb2.ToString();

                string displayText = !string.IsNullOrWhiteSpace(text1) ? text1 : text2;

                if (!string.IsNullOrWhiteSpace(displayText))
                {
                    bool isButton = className.Contains("Button");

                    if (isButton)
                    {
                        buttonCount++;
                        buttonList.Add(displayText);
                        Log("  ✓ 按钮 #" + buttonCount + ": [" + displayText + "]");
                        Log("      类名: " + className);
                        Log("      句柄: 0x" + childHwnd.ToInt64().ToString("X"));
                    }
                    else if (className.Contains("Static") || className.Contains("Edit"))
                    {
                        Log("  ○ 文本控件: [" + displayText + "]");
                        Log("      类名: " + className);
                    }
                }

                return true;
            }, IntPtr.Zero);

            Log("\n  【统计】共 " + controlCount + " 个控件，其中 " + buttonCount + " 个按钮");

            if (buttonCount > 0)
            {
                Console.WriteLine("\n  发现 " + buttonCount + " 个按钮:");
                foreach (var btn in buttonList)
                {
                    Console.WriteLine("    - " + btn);
                }
            }

            Log("");
        }

        static void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, message + "\n");
            }
            catch { }
        }
    }
}
