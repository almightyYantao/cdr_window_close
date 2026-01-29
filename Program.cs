using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace CorelDrawAutoIgnoreError
{
    static class Program
    {
        private static Mutex _mutex;
        private static NotifyIcon _trayIcon;
        private static ErrorDialogMonitor _monitor;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, "CorelDrawErrorMonitor_SingleInstance", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "CorelDRAW Error Monitor is already running!\n\n" +
                    "Check the system tray (near the clock).\n\n" +
                    "程序已在运行！请检查系统托盘。",
                    "Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "CorelDRAW Error Monitor",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("✓ Monitoring Active", null, null).Enabled = false;
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("About / 关于", null, ShowAbout);
            contextMenu.Items.Add("View Log / 查看日志", null, ViewLog);
            contextMenu.Items.Add("Reload Config / 重载配置", null, ReloadConfig);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit / 退出", null, Exit);

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += ShowAbout;

            _monitor = new ErrorDialogMonitor();
            _monitor.Start();

            _trayIcon.ShowBalloonTip(
                3000,
                "CorelDRAW Error Monitor",
                "Monitoring started. Logging to debug.log\n监控已启动,日志保存到debug.log",
                ToolTipIcon.Info);

            Application.Run();

            _monitor.Stop();
            _trayIcon.Dispose();
            _mutex?.ReleaseMutex();
        }

        private static void ShowAbout(object sender, EventArgs e)
        {
            int clickCount = _monitor?.GetAutoClickCount() ?? 0;

            MessageBox.Show(
                $"CorelDRAW Error Monitor v1.0\n\n" +
                $"Status: Monitoring Active\n" +
                $"Auto-clicks: {clickCount}\n\n" +
                $"状态: 监控中\n" +
                $"自动点击次数: {clickCount}\n\n" +
                $"Debug log: debug.log\n" +
                $"Right-click tray icon to view log or exit.\n\n" +
                $"右键托盘图标可查看日志或退出。",
                "About / 关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ViewLog(object sender, EventArgs e)
        {
            string logPath = _monitor?.GetLogPath();
            if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
            {
                try
                {
                    Process.Start("notepad.exe", logPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Cannot open log file:\n{ex.Message}\n\nPath: {logPath}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "Log file not found.\n\n日志文件未找到。",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private static void ReloadConfig(object sender, EventArgs e)
        {
            _monitor?.ReloadConfig();
            MessageBox.Show(
                "Configuration reloaded!\n\n配置已重新加载!",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
