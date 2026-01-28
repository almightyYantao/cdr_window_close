@echo off
chcp 65001 >nul
echo ========================================
echo   CorelDRAW 自动忽略错误插件 - 卸载程序
echo ========================================
echo.

REM 检查是否以管理员权限运行
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 错误: 需要管理员权限!
    echo.
    echo 请按以下步骤操作:
    echo 1. 右键点击此文件 (uninstall.bat)
    echo 2. 选择"以管理员身份运行"
    echo.
    pause
    exit /b 1
)

echo ✓ 已获取管理员权限
echo.

REM 获取当前目录和DLL路径
set INSTALL_DIR=%~dp0
set DLL_PATH=%INSTALL_DIR%CorelDrawAutoIgnoreError.dll

REM 尝试从安装日志读取路径
if exist "%INSTALL_DIR%install.log" (
    set /p DLL_PATH=<"%INSTALL_DIR%install.log"
)

echo 正在卸载插件...
echo 插件路径: %DLL_PATH%
echo.

choice /C YN /M "确认要卸载插件吗"
if errorlevel 2 (
    echo.
    echo 已取消卸载操作。
    pause
    exit /b 0
)

echo.
echo 正在注销 COM 组件...

REM 尝试使用64位regasm
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" (
    "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" /unregister "%DLL_PATH%"
) else (
    "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" /unregister "%DLL_PATH%"
)

echo ✓ COM组件已注销
echo.

REM 删除安装日志
if exist "%INSTALL_DIR%install.log" (
    del "%INSTALL_DIR%install.log"
    echo ✓ 安装记录已删除
)

echo.
echo ========================================
echo ✓ 插件已成功卸载!
echo ========================================
echo.
echo 📋 后续步骤:
echo   1. 重启 CorelDRAW 使更改生效
echo   2. 如需彻底清理,可删除整个插件文件夹
echo.
echo 插件文件夹: %INSTALL_DIR%
echo.

choice /C YN /M "是否现在删除插件文件"
if errorlevel 2 goto :end

echo.
echo 请在关闭此窗口后手动删除文件夹:
echo %INSTALL_DIR%
echo.

:end
echo.
echo 卸载完成! 按任意键退出...
pause >nul
