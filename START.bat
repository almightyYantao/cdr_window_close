@echo off
title CorelDRAW Error Monitor with GDI Hook

REM Check for Administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    cls
    echo.
    echo ==========================================
    echo   CorelDRAW Error Monitor
    echo ==========================================
    echo.
    echo [ERROR] Administrator privileges required!
    echo.
    echo Please right-click START.bat and select
    echo "Run as Administrator"
    echo.
    pause
    exit /b 1
)

REM Check if exe exists
if not exist "CorelDrawErrorMonitor.exe" (
    cls
    echo.
    echo ==========================================
    echo   CorelDRAW Error Monitor
    echo ==========================================
    echo.
    echo [ERROR] CorelDrawErrorMonitor.exe not found!
    echo.
    echo Please make sure you have extracted all files.
    echo.
    pause
    exit /b 1
)

REM Check if GdiHook.dll exists
if not exist "GdiHook.dll" (
    cls
    echo.
    echo ==========================================
    echo   CorelDRAW Error Monitor
    echo ==========================================
    echo.
    echo [ERROR] GdiHook.dll not found!
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
echo ==========================================
echo   CorelDRAW Error Monitor Started!
echo ==========================================
echo.
echo [OK] Running with Administrator privileges
echo [OK] GDI Hook enabled
echo.
echo The program is now running in the system tray.
echo Look near the clock to find the icon.
echo.
echo Features:
echo - Auto-detects CorelDRAW process
echo - Injects GDI hook for precise text capture
echo - Automatically dismisses error dialogs
echo.
echo To exit: Right-click the tray icon - Exit
echo.
echo ==========================================
echo.
timeout /t 5
