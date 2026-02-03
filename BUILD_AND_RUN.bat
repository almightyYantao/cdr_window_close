@echo off
echo ===== 编译按钮检测工具 =====
echo.

dotnet build ButtonDetector.csproj -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===== 编译成功 =====
    echo.
    echo 运行工具...
    echo.
    bin\Release\net472\ButtonDetector.exe
) else (
    echo.
    echo ===== 编译失败 =====
    pause
)
