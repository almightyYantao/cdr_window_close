using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CorelDrawAutoIgnoreError
{
    /// <summary>
    /// CorelDRAW插件主类 - 简化版本
    /// 注意: 这个版本不依赖CorelDRAW COM API,可以在GitHub Actions中编译
    /// 插件会在CorelDRAW加载时自动启动错误监控器
    /// </summary>
    [ComVisible(true)]
    [Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")] // 请生成一个唯一的GUID
    [ProgId("CorelDrawAutoIgnoreError.CorelDrawPlugin")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CorelDrawPlugin
    {
        private ErrorDialogMonitor _errorMonitor;
        private static CorelDrawPlugin _instance;

        /// <summary>
        /// 插件构造函数
        /// </summary>
        public CorelDrawPlugin()
        {
            _instance = this;
        }

        /// <summary>
        /// 插件启动 - CorelDRAW会调用这个方法
        /// </summary>
        [ComVisible(true)]
        public void OnConnection(object application, int connectMode)
        {
            try
            {
                // 初始化错误对话框监控器
                _errorMonitor = new ErrorDialogMonitor();
                _errorMonitor.Start();

                // 显示成功加载的绿色提示框
                MessageBox.Show(
                    "✓ Plugin Loaded Successfully!\n\n" +
                    "The error dialog auto-ignore feature is now active.\n\n" +
                    "When you open CDR files with errors, the error dialogs\n" +
                    "will be automatically dismissed by clicking 'Ignore'.\n\n" +
                    "插件加载成功！\n" +
                    "错误对话框自动忽略功能已启用。",
                    "CorelDRAW Auto Ignore Error Plugin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"✗ Plugin Loading Failed!\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"插件加载失败: {ex.Message}",
                    "CorelDRAW Plugin Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 插件卸载
        /// </summary>
        [ComVisible(true)]
        public void OnDisconnection(int disconnectMode)
        {
            try
            {
                if (_errorMonitor != null)
                {
                    _errorMonitor.Stop();
                    _errorMonitor = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"插件卸载失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 启动监控器 - 可以从外部调用
        /// </summary>
        [ComVisible(true)]
        public void StartMonitoring()
        {
            if (_errorMonitor == null)
            {
                _errorMonitor = new ErrorDialogMonitor();
            }
            _errorMonitor.Start();
        }

        /// <summary>
        /// 停止监控器
        /// </summary>
        [ComVisible(true)]
        public void StopMonitoring()
        {
            _errorMonitor?.Stop();
        }

        /// <summary>
        /// 注册COM类别(用于作为加载项)
        /// </summary>
        [ComRegisterFunction]
        public static void RegisterFunction(Type type)
        {
            // 不需要特殊的注册步骤
        }

        /// <summary>
        /// 注销COM类别
        /// </summary>
        [ComUnregisterFunction]
        public static void UnregisterFunction(Type type)
        {
            // 不需要特殊的注销步骤
        }
    }
}
