# CorelDRAW 按钮检测工具 - 最小化部署包

## 需要复制到Windows的文件

只需要这4个文件：

1. `SimpleButtonDetector.cs` - 核心检测代码
2. `TestProgram.cs` - 主程序入口
3. `ButtonDetector.csproj` - 项目配置
4. `BUILD_AND_RUN.bat` - 一键编译运行

## 使用步骤

1. 创建一个新文件夹（比如 `ButtonDetector`）
2. 把上面4个文件复制到该文件夹
3. 打开CorelDRAW，触发错误对话框
4. 双击运行 `BUILD_AND_RUN.bat`
5. 选择扫描模式
6. 查看生成的 `button_detection.log`

## 系统要求

- Windows 10/11
- .NET Framework 4.7.2 或更高版本（Windows 10/11自带）

## 注意

- 不需要管理员权限
- 不需要安装其他依赖
- 如果没有dotnet命令，需要安装.NET SDK
