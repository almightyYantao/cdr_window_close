# GDI Hook 构建说明

## 准备工作

### 1. 下载MinHook
```bash
git clone https://github.com/TsudaKageyu/minhook.git
cd minhook
mkdir build && cd build
cmake .. -A x64
cmake --build . --config Release
```

编译后会生成 `libMinHook.x64.lib`

### 2. 项目结构
```
GdiHook/
├── GdiHook.cpp          # Hook实现
├── MinHook.h            # 从minhook/include复制
├── libMinHook.x64.lib   # 从minhook/build/Release复制
└── build.bat            # 构建脚本
```

### 3. 构建DLL

使用Visual Studio Developer Command Prompt:

```batch
cl /LD /O2 /EHsc GdiHook.cpp /link /DLL /OUT:GdiHook.dll libMinHook.x64.lib gdi32.lib user32.lib
```

或者使用提供的 build.bat 脚本。

## 使用方法

1. 编译生成 `GdiHook.dll`
2. C#程序将DLL注入到CorelDRAW进程
3. Hook自动捕获所有GDI文本绘制
4. 文本通过共享内存传递给C#程序
5. C#程序根据文本内容决定是否点击按钮

## 调试

Hook会在CorelDRAW.exe同目录生成 `.hook.log` 文件,记录所有捕获的文本。
