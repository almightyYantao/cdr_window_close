using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

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
            // 检查是否已运行
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

            // 创建托盘图标
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "CorelDRAW Error Monitor",
                Visible = true
            };

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("✓ Monitoring Active", null, null).Enabled = false;
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("About / 关于", null, ShowAbout);
            contextMenu.Items.Add("Reload Config / 重载配置", null, ReloadConfig);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit / 退出", null, Exit);

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += ShowAbout;

            // 启动监控
            _monitor = new ErrorDialogMonitor();
            _monitor.Start();

            // 显示启动提示
            _trayIcon.ShowBalloonTip(
                3000,
                "CorelDRAW Error Monitor",
                "Monitoring started. Auto-clicking error dialogs.\n监控已启动,自动处理错误对话框。",
                ToolTipIcon.Info);

            Application.Run();

            // 清理
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
                $"This program automatically clicks buttons\n" +
                $"on CorelDRAW error dialogs.\n\n" +
                $"状态: 监控中\n" +
                $"自动点击次数: {clickCount}\n\n" +
                $"Right-click tray icon to exit.\n" +
                $"右键托盘图标可退出。",
                "About / 关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
