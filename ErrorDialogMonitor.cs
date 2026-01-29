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
        private string _logPath;
        private int _scanCount = 0;

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

        [DllImport("user32.dll")]
        private static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        private const uint BM_CLICK = 0x00F5;

        public ErrorDialogMonitor()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
            LoadConfig();
        }

        private void LoadConfig()
        {
            _config = Config.Load(_configPath);
            LogDebug($"加载了 {_config.DialogRules.Count} 个对话框规则");
            foreach (var rule in _config.DialogRules)
            {
                LogDebug($"  - {rule.Name}: 标题包含[{string.Join(",", rule.WindowTitleContains)}] 内容包含[{string.Join(",", rule.ContentContains)}] → 点击'{rule.ButtonToClick}'");
            }
        }

        private void LogDebug(string message)
        {
            try
            {
                string log = $"[{DateTime.Now:HH:mm:ss}] {message}";
                Debug.WriteLine(log);
                File.AppendAllText(_logPath, log + Environment.NewLine);
            }
            catch { }
        }

        public void Start()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
            LogDebug("监控已启动");
        }

        public void Stop()
        {
            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _monitorTask?.Wait(TimeSpan.FromSeconds(2));
            LogDebug("监控已停止");
        }

        private void MonitorLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _scanCount++;

                    // 每10秒记录一次扫描状态
                    if (_scanCount % 100 == 0)
                    {
                        LogDebug($"[扫描中] 已扫描 {_scanCount} 次，自动点击 {_autoClickCount} 次");
                        LogAllVisibleWindows(); // 列出所有可见窗口
                    }

                    foreach (var rule in _config.DialogRules)
                    {
                        IntPtr dialog = FindDialog(rule);

                        if (dialog != IntPtr.Zero && IsWindowVisible(dialog))
                        {
                            LogDebug($"✓✓✓ 发现匹配规则: {rule.Name}");
                            LogWindowDetails(dialog);

                            if (ClickButton(dialog, rule.ButtonToClick))
                            {
                                _autoClickCount++;
                                LogDebug($"✓ 成功点击 '{rule.ButtonToClick}' 按钮 (第{_autoClickCount}次)");
                                Thread.Sleep(500);
                            }
                            else
                            {
                                LogDebug($"✗ 未找到按钮 '{rule.ButtonToClick}'");
                            }
                        }
                    }

                    Thread.Sleep(_config.Settings.CheckInterval);
                }
                catch (Exception ex)
                {
                    LogDebug($"监控错误: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        private void LogAllVisibleWindows()
        {
            LogDebug("--- 当前所有可见窗口 ---");

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder titleSb = new StringBuilder(256);
                    GetWindowText(hWnd, titleSb, titleSb.Capacity);
                    string title = titleSb.ToString();

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        LogDebug($"  窗口: [{title}]");

                        // 记录这个窗口的子控件文本
                        var childTexts = new System.Collections.Generic.List<string>();
                        EnumChildWindows(hWnd, (childHwnd, childLParam) =>
                        {
                            StringBuilder textSb = new StringBuilder(512);
                            GetWindowText(childHwnd, textSb, textSb.Capacity);
                            string text = textSb.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                childTexts.Add(text);
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (childTexts.Count > 0)
                        {
                            LogDebug($"    子控件文本: {string.Join(", ", childTexts)}");
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            LogDebug("--- 列表结束 ---");
        }

        private void LogWindowDetails(IntPtr hwnd)
        {
            StringBuilder titleSb = new StringBuilder(256);
            GetWindowText(hwnd, titleSb, titleSb.Capacity);
            LogDebug($"  窗口标题: [{titleSb}]");

            LogDebug("  子控件:");
            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                StringBuilder textSb = new StringBuilder(512);
                StringBuilder classSb = new StringBuilder(256);
                GetWindowText(childHwnd, textSb, textSb.Capacity);
                GetClassName(childHwnd, classSb, classSb.Capacity);

                string text = textSb.ToString();
                string className = classSb.ToString();

                if (!string.IsNullOrWhiteSpace(text) || className.Contains("Button"))
                {
                    LogDebug($"    [{className}] \"{text}\"");
                }
                return true;
            }, IntPtr.Zero);
        }

        private IntPtr FindDialog(DialogRule rule)
        {
            IntPtr result = IntPtr.Zero;
            bool debugDetailed = _scanCount % 100 == 1; // 每10秒详细检查一次

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder titleSb = new StringBuilder(256);
                GetWindowText(hWnd, titleSb, titleSb.Capacity);
                string title = titleSb.ToString();

                if (string.IsNullOrWhiteSpace(title)) return true;

                // 检查窗口标题
                bool titleMatch = rule.WindowTitleContains.Any(keyword =>
                    title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                // 检查窗口内容
                bool contentMatch = ContainsContent(hWnd, rule.ContentContains);

                // 详细调试:显示每个窗口的匹配情况
                if (debugDetailed && (titleMatch || contentMatch))
                {
                    LogDebug($"[规则:{rule.Name}] 窗口[{title}] 标题匹配={titleMatch} 内容匹配={contentMatch}");
                }

                if (titleMatch && contentMatch)
                {
                    result = hWnd;
                    return false;
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

                if (keywords.Any(keyword => text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
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

                // 更宽松的匹配
                if (!string.IsNullOrWhiteSpace(text) &&
                    (text.Equals(buttonText, StringComparison.OrdinalIgnoreCase) ||
                     text.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     buttonText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    LogDebug($"    找到按钮: \"{text}\" (匹配 \"{buttonText}\")");
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
            LogDebug("配置已重新加载");
        }

        public string GetLogPath() => _logPath;
    }
}
