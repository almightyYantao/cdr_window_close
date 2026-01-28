@echo off
chcp 65001 >nul
echo ========================================
echo CorelDRAW 自动忽略错误插件 - 编译脚本
echo ========================================
echo.
echo 此脚本用于开发者编译和打包插件
echo 普通用户请使用 installer\install.bat 安装
echo.
pause

echo [1/5] 清理旧文件...
if exist bin\Release rmdir /s /q bin\Release
if exist obj rmdir /s /q obj
if exist deploy rmdir /s /q deploy
echo ✓ 清理完成
echo.

echo [2/5] 编译项目...
dotnet build CorelDrawAutoIgnoreError.csproj -c Release

if %errorLevel% neq 0 (
    echo.
    echo ❌ 编译失败! 请检查错误信息。
    pause
    exit /b 1
)
echo ✓ 编译成功
echo.

echo [3/5] 创建部署包...
mkdir deploy
copy bin\Release\net472\CorelDrawAutoIgnoreError.dll deploy\
copy installer\install.bat deploy\
copy installer\uninstall.bat deploy\
copy installer\使用说明.txt deploy\
copy README.md deploy\

if %errorLevel% neq 0 (
    echo.
    echo ❌ 创建部署包失败!
    pause
    exit /b 1
)
echo ✓ 部署包创建成功
echo.

echo [4/5] 创建ZIP压缩包...
powershell Compress-Archive -Force -Path deploy\* -DestinationPath CorelDRAW-AutoIgnoreError-Plugin.zip

if %errorLevel% neq 0 (
    echo.
    echo ❌ 创建ZIP失败!
    pause
    exit /b 1
)
echo ✓ ZIP压缩包创建成功
echo.

echo [5/5] 生成安装说明...
echo.
echo ========================================
echo ✓ 编译打包完成!
echo ========================================
echo.
echo 📦 发布文件:
echo   - CorelDRAW-AutoIgnoreError-Plugin.zip
echo.
echo 📁 部署包内容:
echo   - CorelDrawAutoIgnoreError.dll (插件主文件)
echo   - install.bat (用户安装脚本)
echo   - uninstall.bat (卸载脚本)
echo   - 使用说明.txt (用户说明)
echo   - README.md (详细文档)
echo.
echo 📤 分发方法:
echo   1. 将 CorelDRAW-AutoIgnoreError-Plugin.zip 发送给用户
echo   2. 用户解压后,右键点击 install.bat 以管理员身份运行
echo   3. 重启CorelDRAW即可使用
echo.
echo 💾 输出位置:
echo   %cd%\CorelDRAW-AutoIgnoreError-Plugin.zip
echo   %cd%\deploy\
echo.
pause
