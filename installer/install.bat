@echo off
chcp 65001 >nul
echo ========================================
echo   CorelDRAW 自动忽略错误插件 - 安装程序
echo ========================================
echo.

REM 检查是否以管理员权限运行
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 错误: 需要管理员权限!
    echo.
    echo 请按以下步骤操作:
    echo 1. 右键点击此文件 (install.bat)
    echo 2. 选择"以管理员身份运行"
    echo.
    pause
    exit /b 1
)

echo ✓ 已获取管理员权限
echo.

REM 获取当前目录
set INSTALL_DIR=%~dp0
set DLL_PATH=%INSTALL_DIR%CorelDrawAutoIgnoreError.dll

echo 正在检查文件...
if not exist "%DLL_PATH%" (
    echo.
    echo ❌ 错误: 找不到插件文件!
    echo 请确保 CorelDrawAutoIgnoreError.dll 与此安装脚本在同一目录下。
    echo.
    pause
    exit /b 1
)

echo ✓ 插件文件检查通过
echo.

echo 正在检查 .NET Framework...
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo ⚠ 警告: 未检测到 .NET Framework 4.7.2 或更高版本
    echo 插件可能无法正常工作,请先安装 .NET Framework 4.7.2
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet-framework/net472
    echo.
    choice /C YN /M "是否继续安装"
    if errorlevel 2 exit /b 1
) else (
    echo ✓ .NET Framework 检查通过
)
echo.

echo ========================================
echo 开始安装插件...
echo ========================================
echo.

echo [1/3] 注册 COM 组件...
REM 尝试使用64位regasm
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" (
    "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" /codebase "%DLL_PATH%"
    if %errorLevel% neq 0 (
        echo.
        echo ⚠ 64位注册失败,尝试使用32位...
        "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" /codebase "%DLL_PATH%"
        if %errorLevel% neq 0 (
            echo.
            echo ❌ COM组件注册失败!
            echo 请检查 .NET Framework 是否正确安装。
            pause
            exit /b 1
        )
    )
) else (
    "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" /codebase "%DLL_PATH%"
    if %errorLevel% neq 0 (
        echo.
        echo ❌ COM组件注册失败!
        pause
        exit /b 1
    )
)

echo ✓ COM组件注册成功
echo.

echo [2/3] 创建安装记录...
echo %DLL_PATH% > "%INSTALL_DIR%install.log"
echo %date% %time% >> "%INSTALL_DIR%install.log"
echo ✓ 安装记录已创建
echo.

echo [3/3] 显示配置说明...
echo.

echo ========================================
echo ✓ 插件安装成功!
echo ========================================
echo.
echo 📋 下一步操作:
echo.
echo 【方法1: CorelDRAW自动加载(推荐)】
echo   CorelDRAW可能会在下次启动时自动加载此插件
echo   如果看到加载提示,请允许加载
echo.
echo 【方法2: 手动配置】
echo   1. 打开 CorelDRAW
echo   2. 点击 [工具] → [选项]
echo   3. 展开 [工作区] → [自动化]
echo   4. 点击 [加载/卸载加载项]
echo   5. 添加插件路径: %DLL_PATH%
echo   6. 重启 CorelDRAW
echo.
echo 【验证安装】
echo   启动CorelDRAW后,应该看到提示:
echo   "CorelDRAW自动忽略错误插件已加载"
echo.
echo 📁 插件位置: %DLL_PATH%
echo.
echo ========================================
echo.

choice /C YN /M "是否现在打开CorelDRAW测试"
if errorlevel 2 goto :end

echo.
echo 正在启动CorelDRAW...

REM 尝试查找CorelDRAW
for %%d in (C D E F) do (
    if exist "%%d:\Program Files\Corel\CorelDRAW Graphics Suite*\Draw\CorelDRW.exe" (
        start "" "%%d:\Program Files\Corel\CorelDRAW Graphics Suite*\Draw\CorelDRW.exe"
        goto :end
    )
    if exist "%%d:\Program Files (x86)\Corel\CorelDRAW Graphics Suite*\Draw\CorelDRW.exe" (
        start "" "%%d:\Program Files (x86)\Corel\CorelDRAW Graphics Suite*\Draw\CorelDRW.exe"
        goto :end
    )
)

echo.
echo 未找到CorelDRAW安装路径,请手动启动CorelDRAW
echo.

:end
echo.
echo 安装完成! 按任意键退出...
pause >nul
