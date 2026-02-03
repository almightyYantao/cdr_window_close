# CorelDRAW 错误对话框自动关闭工具

自动检测并关闭 CorelDRAW 错误对话框，避免反复手动点击。

## 快速开始

### 方式 1: 下载编译好的程序（推荐）

1. 点击右侧 [Releases](https://github.com/almightyYantao/cdr_window_close/releases)
2. 下载最新的 `CorelDRAW-Error-Monitor.zip`
3. 解压到任意文件夹
4. 双击 `START.bat` 启动程序
5. 查看系统托盘（时钟旁边）是否有程序图标

### 方式 2: 触发自动编译

1. 点击 [Actions](https://github.com/almightyYantao/cdr_window_close/actions)
2. 选择 "Build and Release"
3. 点击右侧 "Run workflow" → "Run workflow"
4. 等待编译完成（约 2-3 分钟）
5. 下载生成的 `CorelDRAW-Error-Monitor` 文件

## 功能特点

✅ **自动检测错误对话框** - 每 100ms 扫描一次
✅ **自动点击按钮** - 根据配置自动点击指定按钮
✅ **系统托盘运行** - 不占用任务栏空间
✅ **配置简单** - 通过 JSON 文件配置规则
✅ **无需安装** - 绿色软件，解压即用

## 配置说明

编辑 `config.json` 添加对话框规则：

```json
{
  "dialogRules": [
    {
      "name": "无效的轮廓ID错误",
      "windowTitleContains": ["CorelDRAW"],
      "contentContains": ["无效的轮廓"],
      "buttonToClick": "忽略"
    }
  ],
  "settings": {
    "checkInterval": 100
  }
}
```

详细配置说明请查看 [CONFIG_GUIDE.md](CONFIG_GUIDE.md)

## 调试工具

如果不确定对话框有哪些按钮，可以使用 `ButtonDetectorStandalone.cs` 检测：

```bash
# Windows 上运行
COMPILE.bat

# 或者手动编译
csc /target:exe /out:ButtonDetector.exe ButtonDetectorStandalone.cs
ButtonDetector.exe
```

## 系统要求

- Windows 7/8/10/11
- .NET Framework 4.7.2 或更高版本（Windows 10+ 自带）

## 常见问题

**Q: 程序没有自动关闭对话框？**
A:
1. 检查 `debug.log` 查看匹配情况
2. 使用 `ButtonDetectorStandalone.cs` 检测对话框按钮
3. 调整 `config.json` 中的规则

**Q: 误关闭了其他对话框？**
A: 在 `config.json` 中添加更具体的匹配条件

**Q: 如何停止程序？**
A: 右键托盘图标 → Exit

## 技术架构

```
┌─────────────────────────┐
│  System Tray App        │
│  (ErrorDialogMonitor)   │
└───────────┬─────────────┘
            │
            ├─ 100ms 轮询
            │
            ├─ EnumWindows (扫描所有窗口)
            │
            ├─ 标题匹配 + 按钮特征匹配
            │
            └─ SendMessage(BM_CLICK) 点击按钮
```

## 匹配逻辑

1. **窗口标题匹配** - 检查标题是否包含关键词
2. **内容匹配** - 三种方式：
   - 控件文本匹配（标准 Windows 控件）
   - 按钮组合匹配（GDI 绘制的对话框）
   - 跳过内容检查（最宽松）
3. **点击按钮** - 找到按钮就点击，找不到就发送回车键

## 许可证

MIT License
