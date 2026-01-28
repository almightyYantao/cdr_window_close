@echo off
echo ========================================
echo CorelDRAW 自动忽略错误插件 - 卸载脚本
echo ========================================
echo.

REM 检查是否以管理员权限运行
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo 错误: 需要管理员权限!
    echo 请右键点击此脚本,选择"以管理员身份运行"
    pause
    exit /b 1
)

echo 正在卸载COM组件...
echo.

set DLL_PATH=%cd%\bin\Release\net472\CorelDrawAutoIgnoreError.dll

if not exist "%DLL_PATH%" (
    echo 警告: 找不到DLL文件
    echo 路径: %DLL_PATH%
    echo.
    echo 仍将尝试卸载注册表项...
)

REM 卸载COM组件
"%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" /unregister "%DLL_PATH%"

echo.
echo ========================================
echo 插件已卸载!
echo ========================================
echo.
echo 请重启CorelDRAW以使更改生效。
echo.
pause
