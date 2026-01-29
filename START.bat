@echo off
title CorelDRAW Error Monitor - Quick Start

echo.
echo ========================================
echo   CorelDRAW Error Monitor
echo   Quick Start Guide
echo ========================================
echo.

REM Check if EXE exists
if not exist "CorelDrawErrorMonitor.exe" (
    echo [ERROR] CorelDrawErrorMonitor.exe not found!
    echo.
    echo Please make sure you have extracted all files.
    echo.
    pause
    exit /b 1
)

echo Starting CorelDRAW Error Monitor...
echo.
echo The program will run in the background (system tray).
echo Look for the icon near your clock.
echo.
echo 正在启动监控程序...
echo 程序将在后台运行(系统托盘)。
echo 在时钟附近查找图标。
echo.

start "" "CorelDrawErrorMonitor.exe"

timeout /t 2 /nobreak >nul

echo.
echo ========================================
echo Monitor Started!
echo ========================================
echo.
echo What to do next:
echo.
echo 1. The program is now running in the system tray
echo    (look for the icon near the clock)
echo.
echo 2. Open CorelDRAW and test with problematic files
echo.
echo 3. Error dialogs will be automatically closed
echo.
echo 4. To stop: Right-click the tray icon and select Exit
echo.
echo ========================================
echo.

pause
