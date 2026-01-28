using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Corel.CorelDRAW.Interop.VGCore;

namespace CorelDrawAutoIgnoreError
{
    /// <summary>
    /// CorelDRAW插件主类
    /// </summary>
    [ComVisible(true)]
    [Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")] // 请生成一个唯一的GUID
    [ClassInterface(ClassInterfaceType.None)]
    public class CorelDrawPlugin : IVGAppPlugin
    {
        private Application _corelApp;
        private ErrorDialogMonitor _errorMonitor;

        /// <summary>
        /// 插件启动时调用
        /// </summary>
        public void OnLoad(Application app)
        {
            try
            {
                _corelApp = app;

                // 初始化错误对话框监控器
                _errorMonitor = new ErrorDialogMonitor();
                _errorMonitor.Start();

                // 注册文档打开事件
                _corelApp.OnApplicationEvent += OnApplicationEvent;

                MessageBox.Show("CorelDRAW自动忽略错误插件已加载", "插件信息",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"插件加载失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 插件卸载时调用
        /// </summary>
        public void OnUnload()
        {
            try
            {
                if (_errorMonitor != null)
                {
                    _errorMonitor.Stop();
                    _errorMonitor = null;
                }

                if (_corelApp != null)
                {
                    _corelApp.OnApplicationEvent -= OnApplicationEvent;
                    _corelApp = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"插件卸载失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 应用程序事件处理
        /// </summary>
        private void OnApplicationEvent(string EventName, ref object[] Parameters)
        {
            try
            {
                switch (EventName)
                {
                    case "BeforeDocumentOpen":
                        // 文档打开前，确保错误监控器处于活动状态
                        _errorMonitor?.EnsureMonitoring();
                        break;

                    case "DocumentOpen":
                        // 文档打开后
                        break;
                }
            }
            catch (Exception ex)
            {
                // 记录日志但不影响CorelDRAW运行
                System.Diagnostics.Debug.WriteLine($"事件处理错误: {ex.Message}");
            }
        }
    }
}
