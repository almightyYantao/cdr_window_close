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
            LogDebug("监控已启动 - 扫描间隔: " + _config.Settings.CheckInterval + "ms");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _scanCount++;

                    // 每5分钟记录一次状态（减少日志输出）
                    if (_scanCount % 600 == 0)
                    {
                        LogDebug($"[状态] 已自动点击 {_autoClickCount} 次");
                    }

                    foreach (var rule in _config.DialogRules)
                    {
                        IntPtr dialog = FindDialog(rule);

                        if (dialog != IntPtr.Zero && IsWindowVisible(dialog))
                        {
                            LogDebug($"✓✓✓ 发现匹配规则: {rule.Name}");

                            if (ClickButton(dialog, rule.ButtonToClick))
                            {
                                _autoClickCount++;
                                LogDebug($"✓ 成功点击 '{rule.ButtonToClick}' 按钮 (第{_autoClickCount}次)");
                                Thread.Sleep(500);
                            }
                            else
                            {
                                LogDebug($"✗ 未找到按钮 '{rule.ButtonToClick}'，跳过处理");
                                LogWindowDetails(dialog);
                            }

                            // 找到并处理一个对话框后，跳出循环，避免重复处理
                            break;
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

        private IntPtr FindDialog(DialogRule rule)
        {
            IntPtr result = IntPtr.Zero;

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

                if (!titleMatch) return true;

                // 排除主窗口（主窗口标题包含 " - " 如 "CorelDRAW - 未命名-1"）
                // 但保留对话框（如 "导入 EPS"）
                // 只对包含 "CorelDRAW" 的规则应用此过滤
                if (rule.WindowTitleContains.Any(k => k.Contains("CorelDRAW")) &&
                    title.Contains(" - "))
                {
                    return true;
                }

                // 只在标题匹配时才输出日志和检查内容
                LogDebug($"  [规则:{rule.Name}] 标题匹配成功: [{title}]");

                // 检查窗口内容
                bool contentMatch = ContainsContent(hWnd, rule.ContentContains);

                if (titleMatch && contentMatch)
                {
                    result = hWnd;
                    return false; // 找到匹配窗口，立即停止枚举
                }
                return true;
            }, IntPtr.Zero);

            return result;
        }

        private bool ContainsContent(IntPtr hwnd, System.Collections.Generic.List<string> keywords)
        {
            LogDebug($"  [内容匹配] 开始检查关键词: [{string.Join(", ", keywords)}]");

            // 收集窗口控件文本和按钮文本
            var allTexts = new System.Collections.Generic.List<string>();
            var buttonTexts = new System.Collections.Generic.List<string>();

            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                try
                {
                    StringBuilder classSb = new StringBuilder(256);
                    GetClassName(childHwnd, classSb, classSb.Capacity);
                    string className = classSb.ToString();

                    // 方法1: GetWindowText
                    StringBuilder sb = new StringBuilder(512);
                    GetWindowText(childHwnd, sb, sb.Capacity);
                    string text = sb.ToString();

                    // 方法2: SendMessage WM_GETTEXT
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        StringBuilder sb2 = new StringBuilder(512);
                        SendMessage(childHwnd, WM_GETTEXT, (IntPtr)sb2.Capacity, sb2);
                        text = sb2.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        allTexts.Add(text);

                        // 单独记录按钮文本
                        if (className.Contains("Button"))
                        {
                            buttonTexts.Add(text);
                        }
                    }
                }
                catch { }

                return true;
            }, IntPtr.Zero);

            LogDebug($"    [收集结果] 文本:{allTexts.Count}个, 按钮:{buttonTexts.Count}个");
            if (buttonTexts.Count > 0)
            {
                LogDebug($"    [按钮列表] {string.Join(", ", buttonTexts)}");
            }

            // 如果没有关键词要求，直接通过（只检查按钮）
            if (keywords == null || keywords.Count == 0)
            {
                LogDebug($"    ✓ [无内容要求] 直接通过");
                return true;
            }

            // 检查控件文本是否包含任一关键词
            bool anyKeywordFound = keywords.Any(keyword =>
                allTexts.Any(text => !string.IsNullOrWhiteSpace(text) &&
                    text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));

            if (anyKeywordFound)
            {
                string matchedKeyword = keywords.First(keyword =>
                    allTexts.Any(text => !string.IsNullOrWhiteSpace(text) &&
                        text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));
                LogDebug($"    ✓ [内容匹配成功] 关键词: {matchedKeyword}");
                return true;
            }

            // 检查按钮组合作为兜底方案
            if (buttonTexts.Count > 0)
            {
                LogDebug($"    [按钮组合匹配] 按钮: {string.Join("+", buttonTexts)}");

                // 如果有明确的按钮组合要求，检查是否满足
                // 例如："关于"+"重试"+"忽略" 这种特定的按钮组合
                bool hasExpectedButtons = CheckButtonCombination(buttonTexts, keywords);
                if (hasExpectedButtons)
                {
                    LogDebug($"    ✓ [按钮组合匹配成功]");
                    return true;
                }
            }

            LogDebug($"    ✗ [内容匹配失败]");
            return false;
        }

        private bool CheckButtonCombination(System.Collections.Generic.List<string> buttonTexts, System.Collections.Generic.List<string> keywords)
        {
            // 针对"无效的轮廓ID"错误的特殊按钮组合：关于+重试+忽略
            if (keywords.Any(k => k.Contains("无效的轮廓")))
            {
                bool hasAbout = buttonTexts.Any(t => t.Contains("关于"));
                bool hasRetry = buttonTexts.Any(t => t.Contains("重试"));
                bool hasIgnore = buttonTexts.Any(t => t.Contains("忽略"));

                if (hasAbout && hasRetry && hasIgnore)
                {
                    LogDebug($"      检测到特征按钮组合: 关于+重试+忽略");
                    return true;
                }
            }

            return false;
        }

        private void LogWindowDetails(IntPtr hwnd)
        {
            StringBuilder titleSb = new StringBuilder(256);
            GetWindowText(hwnd, titleSb, titleSb.Capacity);
            LogDebug($"  窗口标题: [{titleSb}]");

            LogDebug("  可用按钮:");
            EnumChildWindows(hwnd, (childHwnd, lParam) =>
            {
                StringBuilder classSb = new StringBuilder(256);
                GetClassName(childHwnd, classSb, classSb.Capacity);

                if (classSb.ToString().Contains("Button"))
                {
                    StringBuilder textSb = new StringBuilder(512);
                    GetWindowText(childHwnd, textSb, textSb.Capacity);
                    if (!string.IsNullOrWhiteSpace(textSb.ToString()))
                    {
                        LogDebug($"    - [{textSb}]");
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private bool ClickButton(IntPtr dialogHwnd, string buttonText)
        {
            bool clicked = false;
            int buttonCount = 0;

            LogDebug($"  [查找按钮] 目标按钮文本: [{buttonText}]");

            EnumChildWindows(dialogHwnd, (childHwnd, lParam) =>
            {
                StringBuilder classSb = new StringBuilder(256);
                GetClassName(childHwnd, classSb, classSb.Capacity);

                // 只处理按钮类型的控件
                if (classSb.ToString().Contains("Button"))
                {
                    buttonCount++;
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(childHwnd, sb, sb.Capacity);
                    string text = sb.ToString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        LogDebug($"    发现按钮 #{buttonCount}: [{text}]");

                        // 更宽松的匹配
                        if (text.Equals(buttonText, StringComparison.OrdinalIgnoreCase) ||
                            text.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            buttonText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            LogDebug($"    ✓ 按钮匹配成功,准备点击: [{text}]");
                            SendMessage(childHwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                            clicked = true;
                            return false;
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (!clicked)
            {
                LogDebug($"  ✗ [查找按钮失败] 共检查了 {buttonCount} 个按钮,未找到匹配的按钮");
            }

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
