using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace CorelDrawAutoIgnoreError
{
    public class ErrorDialogMonitor
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitorTask;
        private bool _isMonitoring;
        private int _autoClickCount = 0;
        private Config _config;
        private string _configPath;

        // Windows API
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        private const uint BM_CLICK = 0x00F5;

        public ErrorDialogMonitor()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            LoadConfig();
        }

        private void LoadConfig()
        {
            _config = Config.Load(_configPath);
            Debug.WriteLine($"加载了 {_config.DialogRules.Count} 个对话框规则");
        }

        public void Start()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
            Debug.WriteLine("监控已启动");
        }

        public void Stop()
        {
            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _monitorTask?.Wait(TimeSpan.FromSeconds(2));
            Debug.WriteLine("监控已停止");
        }

        private void MonitorLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var rule in _config.DialogRules)
                    {
                        IntPtr dialog = FindDialog(rule);

                        if (dialog != IntPtr.Zero && IsWindowVisible(dialog))
                        {
                            Debug.WriteLine($"发现匹配规则: {rule.Name}");

                            if (ClickButton(dialog, rule.ButtonToClick))
                            {
                                _autoClickCount++;
                                Debug.WriteLine($"成功点击 '{rule.ButtonToClick}' 按钮 (第{_autoClickCount}次)");
                                Thread.Sleep(500); // 避免重复处理
                            }
                        }
                    }

                    Thread.Sleep(_config.Settings.CheckInterval);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"监控错误: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        private IntPtr FindDialog(DialogRule rule)
        {
            IntPtr result = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder titleSb = new StringBuilder(256);
                GetWindowText(hWnd, titleSb, titleSb.Capacity);
                string title = titleSb.ToString();

                // 检查窗口标题
                if (rule.WindowTitleContains.Any(keyword => title.Contains(keyword)))
                {
                    // 检查窗口内容
                    if (ContainsContent(hWnd, rule.ContentContains))
                    {
                        result = hWnd;
                        return false; // 停止枚举
                    }
                }
                return true;
            }, IntPtr.Zero);

            return result;
        }

        private bool ContainsContent(IntPtr hwnd, System.Collections.Generic.List<string> keywords)
        {
            bool found = false;

            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(512);
                GetWindowText(childHwnd, sb, sb.Capacity);
                string text = sb.ToString();

                if (keywords.Any(keyword => text.Contains(keyword)))
                {
                    found = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        private bool ClickButton(IntPtr dialogHwnd, string buttonText)
        {
            bool clicked = false;

            EnumChildWindows(dialogHwnd, (childHwnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(childHwnd, sb, sb.Capacity);
                string text = sb.ToString();

                if (text.Equals(buttonText, StringComparison.OrdinalIgnoreCase) ||
                    text.Contains(buttonText))
                {
                    SendMessage(childHwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    clicked = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return clicked;
        }

        public int GetAutoClickCount() => _autoClickCount;

        public void ReloadConfig()
        {
            LoadConfig();
            Debug.WriteLine("配置已重新加载");
        }
    }
}
