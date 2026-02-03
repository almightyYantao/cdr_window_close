# 🔍 CorelDRAW 按钮检测工具 - 极简版

## 📦 只需复制2个文件

```
ButtonDetectorStandalone.cs   (检测代码)
COMPILE.bat                   (一键编译运行)
```

## 🚀 使用方法

### 方法1：一键运行（推荐）

1. 打开CorelDRAW，触发错误对话框
2. 双击 `COMPILE.bat`
3. 按任意键开始扫描
4. 查看 `button_detection.log`

### 方法2：手动编译

```batch
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /out:ButtonDetector.exe ButtonDetectorStandalone.cs
ButtonDetector.exe
```

## 📝 日志示例

```
【窗口】 CorelDRAW (评估版)
  进程ID: 9372

  ✓ 按钮 #1: [关于(&A)]
      类名: Button
  ✓ 按钮 #2: [重试(&R)]
      类名: Button
  ✓ 按钮 #3: [忽略(&I)]
      类名: Button

  ○ 文本控件: [无效的轮廓ID]
      类名: Static
```

## ✅ 优点

- ✅ 单文件，无需项目配置
- ✅ 使用Windows自带编译器
- ✅ 不需要安装任何东西
- ✅ 不需要管理员权限
- ✅ 清晰列出所有按钮

## 📤 发给我

把生成的 `button_detection.log` 文件发给我，我就能知道：
1. CorelDRAW实际使用了什么按钮
2. 为什么有些按钮检测不到
3. 如何准确配置自动化规则
