using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace CorelDrawAutoIgnoreError
{
    /// <summary>
    /// 独立运行的监控程序 - 不需要作为CorelDRAW插件
    /// </summary>
    static class Program
    {
        private static ErrorDialogMonitor _monitor;
        private static NotifyIcon _trayIcon;
        private static bool _isRunning = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 检查是否已经有实例在运行
            bool createdNew;
            using (var mutex = new Mutex(true, "CorelDrawAutoIgnoreErrorMonitor", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "The monitor is already running!\n\n" +
                        "监控程序已经在运行中！\n\n" +
                        "Check the system tray (near the clock).",
                        "Already Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 创建系统托盘图标
                CreateTrayIcon();

                // 启动监控
                StartMonitoring();

                // 显示启动消息
                _trayIcon.ShowBalloonTip(3000,
                    "CorelDRAW Error Monitor",
                    "Monitoring started! Error dialogs will be auto-ignored.\n监控已启动！",
                    ToolTipIcon.Info);

                // 运行应用程序
                Application.Run();
            }
        }

        private static void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "CorelDRAW Auto Ignore Error - Running";
            _trayIcon.Icon = SystemIcons.Application; // 使用系统默认图标

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem("✓ Monitoring Active");
            statusItem.Enabled = false;
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("About / 关于");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("Exit / 退出");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.Visible = true;

            // 双击托盘图标显示关于
            _trayIcon.DoubleClick += (s, e) => ShowAbout();
        }

        private static void StartMonitoring()
        {
            try
            {
                _monitor = new ErrorDialogMonitor();
                _monitor.Start();
                _isRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start monitoring!\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"启动监控失败: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void ShowAbout()
        {
            MessageBox.Show(
                "CorelDRAW Auto Ignore Error Monitor\n" +
                "Version 1.0\n\n" +
                "This program automatically clicks 'Ignore' on CorelDRAW error dialogs.\n\n" +
                "本程序自动点击CorelDRAW错误对话框的'忽略'按钮。\n\n" +
                "Status: " + (_isRunning ? "✓ Running" : "✗ Stopped") + "\n\n" +
                "To exit: Right-click the tray icon and select Exit.\n" +
                "退出: 右键点击托盘图标,选择退出。",
                "About CorelDRAW Error Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ExitApplication()
        {
            var result = MessageBox.Show(
                "Stop monitoring and exit?\n\n" +
                "停止监控并退出程序？",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (_monitor != null)
                {
                    _monitor.Stop();
                }

                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                Application.Exit();
            }
        }
    }
}
