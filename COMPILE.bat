@echo off
echo ===== CorelDRAW 按钮检测工具 - 编译中 =====
echo.

:: 使用Windows自带的C#编译器
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:exe /out:ButtonDetector.exe ButtonDetectorStandalone.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===== 编译成功 =====
    echo.
    echo 运行工具...
    echo.
    ButtonDetector.exe
) else (
    echo.
    echo ===== 编译失败 =====
    echo.
    echo 可能原因：
    echo 1. .NET Framework 4.0+ 未安装
    echo 2. 路径不正确
    echo.
    pause
)
