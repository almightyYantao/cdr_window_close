# CorelDRAW 自动忽略错误插件

## 功能说明

这是一个CorelDRAW的C#插件,用于在打开CDR文件时自动处理错误对话框,特别是针对"包含无效的轮廓ID"这类错误,自动点击"忽略"按钮,避免手动操作。

## 主要特性

- ✅ 自动检测CorelDRAW错误对话框
- ✅ 自动点击"忽略"按钮
- ✅ 后台监控,不影响正常操作
- ✅ 支持中文和英文界面

## 项目结构

```
cdr_window_close/
├── CorelDrawAutoIgnoreError.csproj  # 项目文件
├── CorelDrawPlugin.cs               # 插件主类
├── ErrorDialogMonitor.cs            # 错误对话框监控器
└── README.md                        # 说明文档
```

## 安装步骤

### 1. 编译插件

在Windows环境下,使用Visual Studio或命令行编译:

```bash
# 使用dotnet命令行编译
dotnet build CorelDrawAutoIgnoreError.csproj -c Release
```

### 2. 注册COM组件

编译完成后,需要注册COM组件:

```bash
# 使用管理员权限运行
regasm /codebase CorelDrawAutoIgnoreError.dll
```

### 3. 配置CorelDRAW

1. 打开CorelDRAW
2. 进入 **工具** -> **选项** -> **自动化**
3. 添加编译好的DLL文件路径
4. 重启CorelDRAW

### 4. 手动注册(可选)

如果自动加载不成功,可以手动编辑注册表:

```reg
Windows Registry Editor Version 5.00

[HKEY_CLASSES_ROOT\CLSID\{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}]
@="CorelDRAW Auto Ignore Error Plugin"

[HKEY_CLASSES_ROOT\CLSID\{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}\InprocServer32]
@="C:\\Path\\To\\Your\\CorelDrawAutoIgnoreError.dll"
"ThreadingModel"="Both"
"Class"="CorelDrawAutoIgnoreError.CorelDrawPlugin"
```

> **注意:** 请将路径替换为实际的DLL文件路径,并确保GUID与代码中的一致。

## 工作原理

1. **插件加载**: CorelDRAW启动时自动加载插件
2. **事件监听**: 监听文档打开事件
3. **窗口监控**: 后台线程持续监控错误对话框
4. **自动处理**: 检测到错误对话框后,自动查找并点击"忽略"按钮

## 技术实现

### 核心技术

- **COM Interop**: 使用CorelDRAW的COM API
- **Windows API**: 使用`user32.dll`进行窗口操作
- **多线程**: 后台监控不阻塞主线程

### 关键代码

#### 1. 窗口查找

```csharp
private IntPtr FindErrorDialog()
{
    // 枚举所有可见窗口
    // 检查窗口标题和内容
    // 返回匹配的错误对话框句柄
}
```

#### 2. 按钮点击

```csharp
private bool ClickIgnoreButton(IntPtr dialogHwnd)
{
    // 枚举对话框子窗口
    // 查找"忽略"按钮
    // 发送BM_CLICK消息
}
```

## 重要配置

### 修改GUID

在 [CorelDrawPlugin.cs](CorelDrawPlugin.cs#L11) 中,请生成并替换唯一的GUID:

```csharp
[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")] // 请替换为新的GUID
```

可以在Visual Studio中使用: **工具** -> **创建GUID** 或使用PowerShell:

```powershell
[Guid]::NewGuid()
```

### CorelDRAW版本适配

项目文件中的COM引用需要根据你的CorelDRAW版本调整。常见版本:

- CorelDRAW X7: `VGCore.dll`
- CorelDRAW 2017: `VGCore.dll`
- CorelDRAW 2019: `VGCore.dll`
- CorelDRAW 2020+: `VGCore.dll`

## 故障排除

### 插件未加载

1. 检查DLL是否正确编译为64位(如果CorelDRAW是64位)
2. 确认COM组件已注册: `regasm /codebase YourPlugin.dll`
3. 查看CorelDRAW日志文件

### 按钮未自动点击

1. 检查错误对话框的标题和文本
2. 调整 [ErrorDialogMonitor.cs](ErrorDialogMonitor.cs) 中的关键词匹配逻辑
3. 查看调试输出(`Debug.WriteLine`)

### 调试方法

在Visual Studio中:

1. 附加到CorelDRAW进程
2. 设置断点
3. 查看输出窗口的调试信息

## 注意事项

1. **管理员权限**: 注册COM组件需要管理员权限
2. **杀毒软件**: 某些杀毒软件可能阻止自动化操作
3. **CorelDRAW版本**: 确保代码兼容你的CorelDRAW版本
4. **备份文件**: 使用前建议备份重要的CDR文件

## 兼容性

- **操作系统**: Windows 7/8/10/11
- **CorelDRAW**: X7, 2017, 2019, 2020, 2021, 2022+
- **.NET Framework**: 4.7.2+

## 开发环境

- Visual Studio 2019+
- .NET Framework 4.7.2
- CorelDRAW API

## 许可证

本项目仅供学习和个人使用。

## 联系支持

如有问题,请检查:

1. 错误日志
2. Windows事件查看器
3. CorelDRAW崩溃报告

## 更新日志

### v1.0.0 (2026-01-28)
- 初始版本
- 支持自动点击"忽略"按钮
- 支持中英文界面

## 扩展功能建议

未来可以添加的功能:

- [ ] 配置界面,允许用户选择处理策略(忽略/重试/关闭)
- [ ] 日志记录功能
- [ ] 支持更多类型的错误对话框
- [ ] 错误统计和报告
- [ ] 可配置的监控频率
