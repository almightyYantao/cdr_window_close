using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CorelDrawAutoIgnoreError
{
    /// <summary>
    /// 错误对话框监控器 - 使用Windows API监控并自动处理CorelDRAW错误对话框
    /// </summary>
    public class ErrorDialogMonitor
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitorTask;
        private bool _isMonitoring;

        // Windows API 声明
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        private const uint WM_COMMAND = 0x0111;
        private const uint BM_CLICK = 0x00F5;

        /// <summary>
        /// 启动监控
        /// </summary>
        public void Start()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void Stop()
        {
            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _monitorTask?.Wait(TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// 确保监控处于活动状态
        /// </summary>
        public void EnsureMonitoring()
        {
            if (!_isMonitoring)
            {
                Start();
            }
        }

        /// <summary>
        /// 监控循环
        /// </summary>
        private void MonitorLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 查找CorelDRAW的错误对话框
                    IntPtr errorDialog = FindErrorDialog();

                    if (errorDialog != IntPtr.Zero && IsWindowVisible(errorDialog))
                    {
                        Debug.WriteLine("发现错误对话框,尝试自动点击'忽略'按钮");

                        // 尝试点击"忽略"按钮
                        bool success = ClickIgnoreButton(errorDialog);

                        if (success)
                        {
                            Debug.WriteLine("成功点击'忽略'按钮");
                        }
                        else
                        {
                            Debug.WriteLine("未能找到'忽略'按钮,尝试备用方法");
                            // 尝试备用方法
                            TryAlternativeClick(errorDialog);
                        }
                    }

                    // 检查间隔 - 每100ms检查一次
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"监控循环错误: {ex.Message}");
                    Thread.Sleep(1000); // 出错后等待更长时间
                }
            }
        }

        /// <summary>
        /// 查找CorelDRAW错误对话框
        /// </summary>
        private IntPtr FindErrorDialog()
        {
            // 方法1: 通过窗口标题查找(包含"文件"或"错误"等关键词)
            IntPtr hwnd = FindWindow(null, null);
            IntPtr result = IntPtr.Zero;

            // 枚举所有窗口
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();

                    // 检查是否包含错误相关的关键词
                    if (title.Contains("文件") ||
                        title.Contains(".cdr") ||
                        title.Contains("错误") ||
                        title.Contains("CorelDRAW"))
                    {
                        // 检查窗口内容是否包含"无效的轮廓ID"或"忽略"按钮
                        if (ContainsErrorMessage(hWnd))
                        {
                            result = hWnd;
                            return false; // 停止枚举
                        }
                    }
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            return result;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// 检查窗口是否包含错误消息
        /// </summary>
        private bool ContainsErrorMessage(IntPtr hwnd)
        {
            bool found = false;

            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(512);
                GetWindowText(childHwnd, sb, sb.Capacity);
                string text = sb.ToString();

                if (text.Contains("无效的轮廓ID") ||
                    text.Contains("忽略") ||
                    text.Contains("重试") ||
                    text.Contains("关闭"))
                {
                    found = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        /// <summary>
        /// 点击"忽略"按钮
        /// </summary>
        private bool ClickIgnoreButton(IntPtr dialogHwnd)
        {
            bool buttonClicked = false;

            // 枚举对话框中的所有子窗口,查找"忽略"按钮
            EnumChildWindows(dialogHwnd, (childHwnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(childHwnd, sb, sb.Capacity);
                string buttonText = sb.ToString();

                // 检查是否是"忽略"按钮
                if (buttonText.Contains("忽略") ||
                    buttonText.Equals("Ignore", StringComparison.OrdinalIgnoreCase))
                {
                    // 发送点击消息
                    SendMessage(childHwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    buttonClicked = true;
                    Debug.WriteLine($"找到并点击按钮: {buttonText}");
                    return false; // 停止枚举
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            return buttonClicked;
        }

        /// <summary>
        /// 备用点击方法 - 尝试通过控件ID点击
        /// </summary>
        private void TryAlternativeClick(IntPtr dialogHwnd)
        {
            // Windows对话框标准按钮ID
            // IDIGNORE = 5 (忽略按钮)
            // IDRETRY = 4 (重试按钮)
            // IDABORT = 3 (中止/关闭按钮)

            int[] ignoreButtonIds = { 5, 2 }; // IDIGNORE, IDCANCEL 通常用于忽略

            foreach (int buttonId in ignoreButtonIds)
            {
                IntPtr buttonHwnd = GetDlgItem(dialogHwnd, buttonId);
                if (buttonHwnd != IntPtr.Zero)
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(buttonHwnd, sb, sb.Capacity);
                    string buttonText = sb.ToString();

                    Debug.WriteLine($"尝试点击按钮ID {buttonId}: {buttonText}");

                    if (buttonText.Contains("忽略") ||
                        buttonText.Equals("Ignore", StringComparison.OrdinalIgnoreCase))
                    {
                        SendMessage(buttonHwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                        Debug.WriteLine($"通过备用方法点击了按钮: {buttonText}");
                        break;
                    }
                }
            }
        }
    }
}
