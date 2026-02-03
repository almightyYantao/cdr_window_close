using System;
using System.Windows.Forms;

namespace CorelDrawAutoIgnoreError
{
    class TestProgram
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("=== CorelDRAW 按钮检测工具 ===");
            Console.WriteLine("这个工具会扫描所有CorelDRAW窗口并列出所有按钮");
            Console.WriteLine("日志文件: button_detection.log");
            Console.WriteLine("");
            Console.WriteLine("请确保CorelDRAW已经打开，并且有错误对话框弹出");
            Console.WriteLine("按任意键开始扫描...");
            Console.ReadKey();

            var detector = new SimpleButtonDetector();

            Console.WriteLine("\n选择模式:");
            Console.WriteLine("1 - 单次扫描");
            Console.WriteLine("2 - 连续监控 (每2秒扫描一次)");
            Console.Write("\n请选择 (1/2): ");

            var choice = Console.ReadLine();

            if (choice == "2")
            {
                detector.StartContinuousMonitoring();
            }
            else
            {
                detector.ScanAllWindows();
                Console.WriteLine("\n扫描完成！请查看 button_detection.log 文件");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }
    }
}
