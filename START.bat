@echo off
title CorelDRAW Error Monitor

REM Check if exe exists
if not exist "CorelDrawErrorMonitor.exe" (
    cls
    echo.
    echo ========================================
    echo   CorelDRAW Error Monitor
    echo   Quick Start Guide
    echo ========================================
    echo.
    echo [ERROR] CorelDrawErrorMonitor.exe not found!
    echo.
    echo Please make sure you have extracted all files.
    echo.
    pause
    exit /b 1
)

REM Start the program
start "" "CorelDrawErrorMonitor.exe"

REM Wait a moment
timeout /t 2 /nobreak >nul

REM Show success message
cls
echo.
echo ========================================
echo   CorelDRAW Error Monitor Started!
echo ========================================
echo.
echo The program is now running in the system tray.
echo Look near the clock to find the icon.
echo.
echo To exit: Right-click the tray icon - Exit
echo.
echo ========================================
echo.
timeout /t 3
