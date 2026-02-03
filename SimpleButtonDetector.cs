using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace CorelDrawAutoIgnoreError
{
    public class SimpleButtonDetector
    {
        // Windows API
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        private const uint WM_GETTEXT = 0x000D;
        private string _logPath;

        public SimpleButtonDetector()
        {
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "button_detection.log");
            // 清空旧日志
            File.WriteAllText(_logPath, $"=== 按钮检测日志 {DateTime.Now} ===\n\n");
        }

        public void ScanAllWindows()
        {
            Log("===== 开始扫描所有窗口 =====");

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder titleSb = new StringBuilder(256);
                GetWindowText(hWnd, titleSb, titleSb.Capacity);
                string title = titleSb.ToString();

                if (string.IsNullOrWhiteSpace(title)) return true;

                // 只关注CorelDRAW相关窗口
                if (title.Contains("CorelDRAW"))
                {
                    GetWindowThreadProcessId(hWnd, out uint processId);

                    Log($"\n窗口: [{title}]");
                    Log($"  进程ID: {processId}");
                    Log($"  窗口句柄: 0x{hWnd.ToInt64():X}");

                    // 获取所有控件信息
                    ScanWindowControls(hWnd);
                }

                return true;
            }, IntPtr.Zero);

            Log("\n===== 扫描完成 =====\n");
        }

        private void ScanWindowControls(IntPtr hwnd)
        {
            int controlCount = 0;
            int buttonCount = 0;

            Log("  \n  所有控件列表:");

            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                controlCount++;

                // 获取控件类名
                StringBuilder classSb = new StringBuilder(256);
                GetClassName(childHwnd, classSb, classSb.Capacity);
                string className = classSb.ToString();

                // 获取控件文本 - 方法1: GetWindowText
                StringBuilder textSb1 = new StringBuilder(512);
                GetWindowText(childHwnd, textSb1, textSb1.Capacity);
                string text1 = textSb1.ToString();

                // 获取控件文本 - 方法2: SendMessage WM_GETTEXT
                StringBuilder textSb2 = new StringBuilder(512);
                SendMessage(childHwnd, WM_GETTEXT, (IntPtr)textSb2.Capacity, textSb2);
                string text2 = textSb2.ToString();

                string displayText = !string.IsNullOrWhiteSpace(text1) ? text1 : text2;

                // 记录所有有文本的控件
                if (!string.IsNullOrWhiteSpace(displayText))
                {
                    Log($"    [{controlCount}] 类名={className}, 文本=[{displayText}], 句柄=0x{childHwnd.ToInt64():X}");

                    // 特别标记按钮
                    if (className.Contains("Button"))
                    {
                        buttonCount++;
                        Log($"        ^^^^^ 这是按钮 #{buttonCount} ^^^^^");
                    }
                }

                return true;
            }, IntPtr.Zero);

            Log($"\n  统计: 共 {controlCount} 个控件, 其中 {buttonCount} 个按钮");
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, message + "\n");
                Console.WriteLine(message);
            }
            catch { }
        }

        public void StartContinuousMonitoring()
        {
            Log("\n开始连续监控模式 (按Ctrl+C停止)...\n");

            while (true)
            {
                ScanAllWindows();
                Thread.Sleep(2000); // 每2秒扫描一次
            }
        }
    }
}
