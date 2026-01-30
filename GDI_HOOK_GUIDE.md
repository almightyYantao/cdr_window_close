# GDI Hook 完整实现方案

## 📋 概述

通过DLL注入+GDI Hook技术,精确捕获CorelDRAW错误对话框中GDI绘制的文本内容。

## 🏗️ 架构

```
┌─────────────────────────────────────┐
│     CorelDRAW.exe 进程              │
│  ┌────────────────────────────────┐ │
│  │ GdiHook.dll (注入的)           │ │
│  │  ├─ Hook TextOutW              │ │
│  │  ├─ Hook ExtTextOutW           │ │
│  │  └─ Hook DrawTextW             │ │
│  │       ↓                         │ │
│  │  捕获文本: "包含无效的轮廓 ID"  │ │
│  └────────────────────────────────┘ │
└─────────────────────────────────────┘
            │ 共享内存通信
            ↓
┌─────────────────────────────────────┐
│ CorelDrawErrorMonitor.exe          │
│  ├─ DLL注入器                       │
│  ├─ 共享内存读取                    │
│  ├─ 文本内容匹配                    │
│  └─ 自动点击按钮                    │
└─────────────────────────────────────┘
```

## 📦 文件结构

```
cdr_window_close/
├── GdiHook/                    # C++ Hook DLL项目
│   ├── GdiHook.cpp            # Hook实现
│   ├── MinHook.h              # MinHook头文件
│   ├── libMinHook.x64.lib     # MinHook库
│   ├── build.bat              # 构建脚本
│   └── README.md              # 构建说明
│
├── DllInjector.cs             # DLL注入器
├── GdiTextCapture.cs          # 共享内存读取
├── ErrorDialogMonitor.cs      # 监控主逻辑(修改后)
└── Program.cs                 # 程序入口(修改后)
```

## 🔧 构建步骤

### 第1步: 构建MinHook

```bash
# 克隆MinHook
git clone https://github.com/TsudaKageyu/minhook.git
cd minhook

# 构建(需要CMake和Visual Studio)
mkdir build && cd build
cmake .. -A x64
cmake --build . --config Release

# 复制文件
copy include\MinHook.h ..\cdr_window_close\GdiHook\
copy build\Release\libMinHook.x64.lib ..\cdr_window_close\GdiHook\
```

### 第2步: 构建GdiHook.dll

在Visual Studio Developer Command Prompt中:

```batch
cd GdiHook
cl /LD /O2 /EHsc GdiHook.cpp /link /DLL /OUT:GdiHook.dll libMinHook.x64.lib gdi32.lib user32.lib
```

### 第3步: 构建C#程序

在`.csproj`中添加新文件:

```xml
<ItemGroup>
  <Compile Include="DllInjector.cs" />
  <Compile Include="GdiTextCapture.cs" />
</ItemGroup>

<ItemGroup>
  <!-- 复制GdiHook.dll到输出目录 -->
  <None Update="GdiHook\GdiHook.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

然后编译:

```bash
dotnet build -c Release
```

## 🎯 使用流程

1. **启动Monitor程序**
   ```
   CorelDrawErrorMonitor.exe
   ```

2. **程序自动检测CorelDRAW进程**
   - 如果CorelDRAW正在运行,自动注入GdiHook.dll
   - 如果未运行,等待启动后注入

3. **Hook开始工作**
   - 所有GDI文本绘制都被捕获
   - 文本通过共享内存传递给Monitor

4. **精确内容匹配**
   - Monitor读取实时文本
   - 检测到"包含无效的轮廓"等关键词
   - 自动点击"忽略"按钮

## 🔍 调试

### Hook日志
GdiHook会在CorelDRAW.exe同目录生成日志:
```
CorelDRW.exe.hook.log
```

查看捕获的所有文本。

### Monitor日志
```
debug.log
```

查看注入状态和匹配结果。

## ⚠️ 注意事项

1. **管理员权限**: DLL注入需要管理员权限
2. **杀毒软件**: 可能被误报,需要添加白名单
3. **进程架构**: 确保DLL和CorelDRAW都是64位
4. **CorelDRAW版本**: 不同版本可能使用不同的GDI函数

## 🚀 优势

- ✅ **100%精确**: 直接捕获GDI绘制的原始文本
- ✅ **实时性**: 文本绘制瞬间就被捕获
- ✅ **完整性**: 不受窗口层级或可见性影响

## 📝 下一步

完成此方案后,如果还有其他对话框需要处理,只需在config.json中添加新规则,基于捕获的文本内容精确匹配。
