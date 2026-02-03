# 配置说明 (config.json)

## 简化版匹配规则

现在使用简化的匹配逻辑，无需 GDI Hook：

1. ✅ **窗口标题匹配** - 检查标题是否包含指定文字
2. ✅ **按钮特征匹配** - 检查窗口中是否有特定按钮组合
3. ✅ **内容关键词匹配** - 检查窗口控件文本（如果有的话）

## 配置示例

```json
{
  "dialogRules": [
    {
      "name": "无效的轮廓ID错误",
      "windowTitleContains": ["CorelDRAW"],
      "contentContains": ["无效的轮廓"],
      "buttonToClick": "忽略"
    },
    {
      "name": "自动备份文件提示",
      "windowTitleContains": ["CorelDRAW"],
      "contentContains": ["自动备份"],
      "buttonToClick": "取消"
    }
  ],
  "settings": {
    "checkInterval": 100,
    "showNotifications": false
  }
}
```

## 配置字段说明

### dialogRules (对话框规则数组)

每个规则包含：

- **name** - 规则名称（用于日志识别）
- **windowTitleContains** - 窗口标题必须包含的文字（数组，满足任意一个即可）
- **contentContains** - 窗口内容关键词（数组，满足任意一个即可）
  - 可以为空数组 `[]`，表示不检查内容，只检查标题和按钮
- **buttonToClick** - 要点击的按钮文字

### settings (全局设置)

- **checkInterval** - 检查间隔（毫秒），默认 100ms
- **showNotifications** - 是否显示通知（暂未实现）

## 匹配逻辑

### 1. 窗口标题匹配

```json
"windowTitleContains": ["CorelDRAW"]
```

只要窗口标题包含 "CorelDRAW" 就匹配成功。

### 2. 内容匹配（三种方式）

#### 方式 A：控件文本匹配（如果内容是 Windows 控件）

```json
"contentContains": ["自动备份"]
```

扫描窗口中的所有文本控件，查找是否包含 "自动备份"。

#### 方式 B：按钮组合匹配（兜底方案）

对于"无效的轮廓ID"这种 GDI 绘制的错误：

```json
"contentContains": ["无效的轮廓"]
```

虽然看不到错误文本，但会检查按钮组合：
- 如果同时存在："关于"+"重试"+"忽略" 三个按钮
- 就认为是"无效的轮廓ID"错误

#### 方式 C：不检查内容（最宽松）

```json
"contentContains": []
```

只检查窗口标题和按钮文本，不检查对话框内容。

### 3. 点击按钮

```json
"buttonToClick": "忽略"
```

找到匹配窗口后，自动点击 "忽略" 按钮。

## 实际场景示例

### 场景 1: 无效的轮廓ID 错误

**问题**: CorelDRAW 弹出错误对话框，内容是 GDI 绘制的，无法获取。

**解决方案**: 使用按钮组合识别

```json
{
  "name": "无效的轮廓ID错误",
  "windowTitleContains": ["CorelDRAW"],
  "contentContains": ["无效的轮廓"],
  "buttonToClick": "忽略"
}
```

**匹配过程**:
1. ✓ 窗口标题包含 "CorelDRAW"
2. ✓ 检测到按钮组合: "关于"+"重试"+"忽略"
3. ✓ 点击 "忽略" 按钮

### 场景 2: 只根据窗口标题和按钮

**问题**: 某个对话框没有可识别的文本内容

**解决方案**: 不检查内容，只检查标题和按钮

```json
{
  "name": "某个神秘对话框",
  "windowTitleContains": ["CorelDRAW"],
  "contentContains": [],
  "buttonToClick": "OK"
}
```

**匹配过程**:
1. ✓ 窗口标题包含 "CorelDRAW"
2. ✓ 不检查内容（空数组）
3. ✓ 点击 "OK" 按钮

### 场景 3: 有明确文本控件的对话框

**问题**: 对话框内容是标准 Windows 控件

**解决方案**: 使用文本匹配

```json
{
  "name": "自动备份提示",
  "windowTitleContains": ["CorelDRAW"],
  "contentContains": ["自动备份", "保存"],
  "buttonToClick": "取消"
}
```

**匹配过程**:
1. ✓ 窗口标题包含 "CorelDRAW"
2. ✓ 窗口内容包含 "自动备份" 或 "保存"
3. ✓ 点击 "取消" 按钮

## 调试技巧

### 1. 查看日志

日志文件位置: `debug.log`

日志会显示：
- 扫描到的窗口标题
- 找到的按钮列表
- 匹配成功/失败的原因

### 2. 使用按钮检测工具

如果不确定窗口有哪些按钮，可以使用 `ButtonDetectorStandalone.cs`:

```bash
# 编译
csc /target:exe /out:ButtonDetector.exe ButtonDetectorStandalone.cs

# 运行（打开 CorelDRAW 错误对话框后）
ButtonDetector.exe
```

查看生成的 `button_detection.log`，了解对话框的按钮结构。

## 注意事项

⚠️ **可能误触发**

由于不检查对话框具体内容，可能会误点击其他对话框。建议：

1. `windowTitleContains` 尽量具体
2. `contentContains` 填写特征关键词（即使无法匹配，也能作为日志标识）
3. 定期查看日志，确认自动点击的是正确的对话框

⚠️ **找不到按钮时的行为**

如果找不到指定的按钮，程序会：
1. 激活窗口
2. 发送回车键（Enter）
3. 记录到日志

这对于某些默认按钮是 OK/确定 的对话框很有用。

## 更新配置

修改 `config.json` 后：
1. 右键托盘图标
2. 选择 "重新加载配置"
3. 新配置立即生效，无需重启程序
