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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        private const uint BM_CLICK = 0x00F5;
        private const uint WM_GETTEXT = 0x000D;

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

                        // 记录这个窗口的完整子控件树
                        LogDebug($"    === 开始详细扫描窗口子控件 ===");
                        LogWindowChildrenRecursive(hWnd, 0, 3);
                        LogDebug($"    === 扫描结束 ===");
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
            // 收集所有文本
            var allTexts = new System.Collections.Generic.List<string>();
            CollectAllTextsRecursive(hwnd, allTexts, 0, 3);

            // 方式1: 检查是否包含关键词文本
            foreach (var text in allTexts)
            {
                if (!string.IsNullOrWhiteSpace(text) && keywords.Any(keyword =>
                    text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }

            // 方式2: 对于"无效的轮廓"规则,检查特定的按钮组合
            // 如果关键词包含"无效"并且窗口有"关于"、"重试"、"忽略"三个按钮,则认为匹配
            if (keywords.Any(k => k.Contains("无效")))
            {
                bool hasAbout = allTexts.Any(t => t.Contains("关于"));
                bool hasRetry = allTexts.Any(t => t.Contains("重试"));
                bool hasIgnore = allTexts.Any(t => t.Contains("忽略"));

                if (hasAbout && hasRetry && hasIgnore)
                {
                    // 进一步检查窗口标题,确保不是主窗口
                    StringBuilder titleSb = new StringBuilder(256);
                    GetWindowText(hwnd, titleSb, titleSb.Capacity);
                    string title = titleSb.ToString();

                    // 错误对话框标题不包含" - "(主窗口有" - 文件路径")
                    if (!title.Contains(" - "))
                    {
                        LogDebug($"    [特征匹配] 检测到错误对话框特征: 标题不含路径且有关于/重试/忽略按钮");
                        return true;
                    }
                }
            }

            return false;
        }

        private void CollectAllTextsRecursive(IntPtr hwnd, System.Collections.Generic.List<string> texts, int depth, int maxDepth)
        {
            if (depth >= maxDepth) return;

            try
            {
                EnumChildWindows(hwnd, (childHwnd, lParam) =>
                {
                    try
                    {
                        // 方法1: GetWindowText
                        StringBuilder sb = new StringBuilder(512);
                        GetWindowText(childHwnd, sb, sb.Capacity);
                        string text = sb.ToString();

                        // 方法2: SendMessage WM_GETTEXT (对于某些静态文本控件更有效)
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            StringBuilder sb2 = new StringBuilder(512);
                            SendMessage(childHwnd, WM_GETTEXT, (IntPtr)sb2.Capacity, sb2);
                            text = sb2.ToString();
                        }

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            texts.Add(text);
                        }

                        // 递归检查子控件的子控件(仅前2层,避免性能问题)
                        if (depth < 2)
                        {
                            CollectAllTextsRecursive(childHwnd, texts, depth + 1, maxDepth);
                        }
                    }
                    catch { }

                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        private void LogWindowChildrenRecursive(IntPtr hwnd, int depth, int maxDepth)
        {
            if (depth >= maxDepth) return;

            string indent = new string(' ', depth * 4);
            int childCount = 0;

            try
            {
                EnumChildWindows(hwnd, (childHwnd, lParam) =>
                {
                    try
                    {
                        childCount++;

                        // 获取类名
                        StringBuilder classSb = new StringBuilder(256);
                        GetClassName(childHwnd, classSb, classSb.Capacity);
                        string className = classSb.ToString();

                        // 获取文本 - 方法1
                        StringBuilder textSb = new StringBuilder(512);
                        GetWindowText(childHwnd, textSb, textSb.Capacity);
                        string text = textSb.ToString();

                        // 获取文本 - 方法2
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            StringBuilder textSb2 = new StringBuilder(512);
                            SendMessage(childHwnd, WM_GETTEXT, (IntPtr)textSb2.Capacity, textSb2);
                            text = textSb2.ToString();
                        }

                        // 是否可见
                        bool visible = IsWindowVisible(childHwnd);

                        // 显示控件信息
                        string displayText = string.IsNullOrWhiteSpace(text) ? "(无文本)" : $"\"{text}\"";
                        LogDebug($"{indent}[{childCount}] 类:{className}, 可见:{visible}, 文本:{displayText}");

                        // 递归显示子控件
                        if (depth < maxDepth - 1)
                        {
                            LogWindowChildrenRecursive(childHwnd, depth + 1, maxDepth);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"{indent}[{childCount}] 错误: {ex.Message}");
                    }

                    return true;
                }, IntPtr.Zero);
            }
            catch { }
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
