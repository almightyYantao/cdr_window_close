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
        private bool _dllInjected = false;
        private GdiTextCapture _gdiCapture = null;

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

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        private const uint BM_CLICK = 0x00F5;
        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const int VK_RETURN = 0x0D;
        private const uint KEYEVENTF_KEYUP = 0x0002;

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

            // 尝试注入DLL到CorelDRAW
            TryInjectHookDll();

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
            LogDebug("开始监控循环...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _scanCount++;

                    // 每30秒检查一次CorelDRAW进程并尝试注入(如果之前未注入或进程重启)
                    if (_scanCount % 300 == 0 && !_dllInjected)
                    {
                        LogDebug("[周期检查] 尝试注入GDI Hook DLL...");
                        TryInjectHookDll();
                    }

                    // 每60秒记录一次状态
                    if (_scanCount % 600 == 0)
                    {
                        string hookStatus = _dllInjected ? "已注入" : "未注入";
                        LogDebug($"[状态] 已运行 {_scanCount/10} 秒，自动点击 {_autoClickCount} 次，GDI Hook: {hookStatus}");
                    }

                    // 前10次循环记录日志,帮助调试
                    if (_scanCount <= 10)
                    {
                        LogDebug($"[扫描] 第 {_scanCount} 次扫描");
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
                                LogDebug($"✗ 未找到按钮 '{rule.ButtonToClick}',尝试发送回车键");
                                LogWindowDetails(dialog);

                                // 先激活窗口
                                SetForegroundWindow(dialog);
                                Thread.Sleep(100);

                                // 使用 keybd_event 模拟全局键盘输入
                                keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);  // 按下回车
                                Thread.Sleep(50);
                                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);  // 释放回车

                                _autoClickCount++;
                                LogDebug($"✓ 已发送回车键 (第{_autoClickCount}次)");
                                Thread.Sleep(500);
                            }

                            // 处理完对话框后清空GDI文本缓冲区，避免影响下一个对话框的判断
                            if (_gdiCapture != null)
                            {
                                _gdiCapture.ClearText();
                                LogDebug($"  [GDI缓冲区已清空]");
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

                LogDebug($"  [规则:{rule.Name}] 标题匹配成功: [{title}]");

                // 检查窗口内容
                bool contentMatch = ContainsContent(hWnd, rule.ContentContains);

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
            LogDebug($"  [内容匹配] 开始检查关键词: [{string.Join(", ", keywords)}]");

            // 方式1: 优先使用GDI Hook捕获的文本(精准匹配)
            if (_gdiCapture != null)
            {
                try
                {
                    string gdiText = _gdiCapture.GetLatestText();
                    if (!string.IsNullOrWhiteSpace(gdiText))
                    {
                        LogDebug($"    [GDI文本] {gdiText.Substring(0, Math.Min(80, gdiText.Length))}...");

                        // 检查是否包含任一关键词
                        bool anyMatch = keywords.Any(keyword =>
                            gdiText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (anyMatch)
                        {
                            LogDebug($"    ✓ [GDI匹配成功]");
                            return true;
                        }
                        else
                        {
                            LogDebug($"    ✗ [GDI未匹配]");
                        }
                    }
                    else
                    {
                        LogDebug($"    [GDI文本为空]");
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"    [GDI读取失败] {ex.Message}");
                }
            }
            else
            {
                LogDebug($"    [GDI未初始化]");
            }

            // 方式2: 收集窗口控件文本(兜底方案)
            var allTexts = new System.Collections.Generic.List<string>();
            CollectAllTextsRecursive(hwnd, allTexts, 0, 3);

            if (allTexts.Count > 0)
            {
                LogDebug($"    [控件文本] 共收集到 {allTexts.Count} 个文本: [{string.Join(", ", allTexts.Take(5))}...]");
            }
            else
            {
                LogDebug($"    [控件文本] 未收集到任何文本");
            }

            // 检查是否包含任一关键词
            bool anyKeywordFound = keywords.Any(keyword =>
                allTexts.Any(text => !string.IsNullOrWhiteSpace(text) &&
                    text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));

            if (anyKeywordFound)
            {
                string matchedKeyword = keywords.First(keyword =>
                    allTexts.Any(text => !string.IsNullOrWhiteSpace(text) &&
                        text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));
                LogDebug($"    ✓ [控件匹配成功] 关键词: {matchedKeyword}");
                return true;
            }
            else
            {
                LogDebug($"    ✗ [控件未匹配]");
            }

            // 方式3: 按钮组合特征匹配(最后的兜底方案,仅用于"无效的轮廓"错误)
            // 只对包含"无效的轮廓"的规则启用,避免误匹配其他包含"无效"的规则
            if (keywords.Contains("无效的轮廓"))
            {
                LogDebug($"    [按钮特征] 检查按钮组合...");
                bool hasAbout = allTexts.Any(t => t.Contains("关于"));
                bool hasRetry = allTexts.Any(t => t.Contains("重试"));
                bool hasIgnore = allTexts.Any(t => t.Contains("忽略"));

                LogDebug($"      关于:{hasAbout}, 重试:{hasRetry}, 忽略:{hasIgnore}");

                if (hasAbout && hasRetry && hasIgnore)
                {
                    StringBuilder titleSb = new StringBuilder(256);
                    GetWindowText(hwnd, titleSb, titleSb.Capacity);
                    string title = titleSb.ToString();

                    LogDebug($"      窗口标题检查: [{title}], 包含' - ':{title.Contains(" - ")}");

                    if (!title.Contains(" - "))
                    {
                        LogDebug($"    ✓ [按钮特征匹配成功] 关于+重试+忽略");
                        return true;
                    }
                    else
                    {
                        LogDebug($"    ✗ [按钮特征失败] 标题包含' - '");
                    }
                }
                else
                {
                    LogDebug($"    ✗ [按钮特征失败] 按钮组合不满足");
                }
            }

            LogDebug($"  ✗ [内容匹配失败] 所有匹配方式都失败");
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

        private void TryInjectHookDll()
        {
            try
            {
                // 检测CorelDRAW进程
                int pid = DllInjector.FindCorelDrawProcess();
                if (pid > 0)
                {
                    LogDebug($"检测到CorelDRAW进程 (PID: {pid})");

                    // 获取DLL路径
                    string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GdiHook.dll");
                    if (!File.Exists(dllPath))
                    {
                        LogDebug($"GdiHook.dll未找到: {dllPath}");
                        return;
                    }

                    // 注入DLL
                    if (DllInjector.InjectDll(pid, dllPath))
                    {
                        LogDebug("GDI Hook DLL注入成功");
                        _dllInjected = true;

                        // 等待Hook初始化并创建共享内存
                        Thread.Sleep(1000);

                        // 初始化共享内存读取（重试最多3次）
                        _gdiCapture = new GdiTextCapture();
                        bool connected = false;
                        for (int i = 0; i < 3; i++)
                        {
                            if (_gdiCapture.Initialize())
                            {
                                LogDebug($"共享内存连接成功 (第{i+1}次尝试)");
                                connected = true;
                                break;
                            }
                            else
                            {
                                LogDebug($"共享内存连接失败 (第{i+1}次尝试), 等待后重试...");
                                Thread.Sleep(500);
                            }
                        }

                        if (!connected)
                        {
                            LogDebug("共享内存连接失败，GDI文本捕获将不可用");
                            LogDebug("可能原因: 权限问题或Hook初始化失败");
                        }
                    }
                    else
                    {
                        LogDebug("GDI Hook DLL注入失败");
                    }
                }
                else
                {
                    LogDebug("未检测到CorelDRAW进程,将在检测到后自动注入");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"注入DLL时发生错误: {ex.Message}");
            }
        }
    }
}
