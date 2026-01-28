# 编译和安装说明

## 快速开始(Windows环境)

### 方法1: 使用提供的批处理脚本(推荐)

1. **右键点击** `build.bat`,选择 **"以管理员身份运行"**
2. 等待编译和注册完成
3. 按照提示配置CorelDRAW
4. 重启CorelDRAW

### 方法2: 手动编译

#### 前置条件

- Windows 7/8/10/11
- Visual Studio 2019+ 或 .NET Framework 4.7.2 SDK
- CorelDRAW(任意支持插件的版本)

#### 步骤

1. **安装.NET Framework开发工具**

   如果还没有安装,下载安装:
   - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/community/)
   - 或者 [.NET Framework 4.7.2 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net472)

2. **编译项目**

   打开命令提示符(管理员),进入项目目录:

   ```bash
   cd C:\path\to\cdr_window_close
   dotnet build CorelDrawAutoIgnoreError.csproj -c Release
   ```

   或者在Visual Studio中:
   - 打开 `CorelDrawAutoIgnoreError.csproj`
   - 选择 **Release** 配置
   - 点击 **生成** -> **生成解决方案**

3. **注册COM组件**

   使用管理员权限运行命令:

   ```bash
   cd bin\Release\net472
   "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" /codebase CorelDrawAutoIgnoreError.dll
   ```

4. **配置CorelDRAW**

   有两种方法加载插件:

   **方法A: 通过CorelDRAW界面(推荐)**

   1. 打开CorelDRAW
   2. 点击 **工具** -> **选项**
   3. 展开 **工作区** -> **自动化**
   4. 点击 **加载/卸载加载项**
   5. 添加编译好的DLL路径
   6. 重启CorelDRAW

   **方法B: 修改注册表**

   1. 编辑 `register_plugin.reg` 文件
   2. 替换GUID为你在代码中使用的GUID
   3. 替换DLL路径为实际路径
   4. 双击运行 `register_plugin.reg`
   5. 重启CorelDRAW

## 验证安装

1. 启动CorelDRAW
2. 应该看到提示消息:"CorelDRAW自动忽略错误插件已加载"
3. 尝试打开一个会产生错误的CDR文件
4. 错误对话框应该自动关闭

## 卸载插件

### 使用批处理脚本

右键点击 `uninstall.bat`,选择 **"以管理员身份运行"**

### 手动卸载

```bash
cd bin\Release\net472
"%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" /unregister CorelDrawAutoIgnoreError.dll
```

## 常见问题

### Q1: 编译时提示找不到CorelDRAW类型库

**解决方案:**

1. 找到CorelDRAW安装目录中的 `VGCore.tlb` 或 `VGCore.dll`
   - 通常在: `C:\Program Files\Corel\CorelDRAW Graphics Suite XXX\Draw\`

2. 修改 `.csproj` 文件中的COM引用路径,或者在Visual Studio中:
   - 右键项目 -> **添加** -> **引用**
   - 选择 **COM** 标签页
   - 找到 **CorelDRAW XX.X Type Library**
   - 添加引用

### Q2: 插件未加载

**检查清单:**

- [ ] DLL是否编译为64位(如果CorelDRAW是64位版本)
- [ ] COM组件是否成功注册(查看regasm输出)
- [ ] CorelDRAW是否重启
- [ ] 杀毒软件是否阻止了插件
- [ ] Windows事件查看器中是否有错误日志

**调试方法:**

在Visual Studio中:
1. 打开项目
2. 点击 **调试** -> **附加到进程**
3. 选择 `CorelDRW.exe` 进程
4. 在代码中设置断点
5. 在CorelDRAW中触发操作

### Q3: 自动点击不工作

**可能原因:**

1. 错误对话框的文本或结构与预期不同
2. 需要调整监控频率或匹配关键词

**解决方案:**

1. 编辑 `ErrorDialogMonitor.cs`
2. 修改 `FindErrorDialog()` 和 `ContainsErrorMessage()` 方法中的关键词匹配逻辑
3. 调整监控间隔(默认100ms)

```csharp
// 在ErrorDialogMonitor.cs中修改
if (title.Contains("文件") ||
    title.Contains("您的错误关键词") ||  // 添加更多关键词
    title.Contains(".cdr"))
{
    // ...
}
```

### Q4: CorelDRAW崩溃

**安全措施:**

1. 备份重要的CDR文件
2. 在代码中添加更多try-catch保护
3. 检查是否有线程安全问题
4. 降低监控频率

### Q5: 不同CorelDRAW版本兼容性

不同版本的CorelDRAW可能需要不同的配置:

**CorelDRAW X7-2017:**
- 注册表路径: `HKEY_LOCAL_MACHINE\SOFTWARE\Corel\CorelDRAW\17.0\`

**CorelDRAW 2018-2019:**
- 注册表路径: `HKEY_LOCAL_MACHINE\SOFTWARE\Corel\CorelDRAW\20.0\`

**CorelDRAW 2020+:**
- 注册表路径: `HKEY_LOCAL_MACHINE\SOFTWARE\Corel\CorelDRAW\23.0\`

在 `register_plugin.reg` 中相应修改版本号。

## 开发说明

### 生成新的GUID

在PowerShell中运行:

```powershell
[Guid]::NewGuid()
```

或在Visual Studio中:
- **工具** -> **创建GUID**
- 选择格式4(Registry Format)
- 点击 **复制**

然后替换 `CorelDrawPlugin.cs` 中的GUID:

```csharp
[Guid("YOUR-NEW-GUID-HERE")]
```

### 调试输出

插件会输出调试信息,可以在Visual Studio的输出窗口查看,或使用 [DebugView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview) 工具查看。

### 修改监控逻辑

如果需要处理不同类型的错误对话框:

1. 编辑 `ErrorDialogMonitor.cs`
2. 在 `FindErrorDialog()` 中添加新的匹配规则
3. 在 `ClickIgnoreButton()` 中调整按钮识别逻辑

## 性能优化

- 监控间隔: 默认100ms,可以根据需要调整
- 只在需要时启动监控: 通过 `BeforeDocumentOpen` 事件触发
- 使用异步监控: 不阻塞主线程

## 许可和免责声明

本项目仅供学习和个人使用。使用本插件造成的任何数据丢失或损坏,开发者不承担责任。请务必备份重要文件。
