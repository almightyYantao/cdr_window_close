@echo off
setlocal enabledelayedexpansion
title CorelDRAW Plugin Installer

echo.
echo ========================================
echo   CorelDRAW Auto Ignore Error Plugin
echo   Installer v1.0
echo ========================================
echo.

REM === DEBUG: Show paths ===
echo [DEBUG]
echo Current working directory: %CD%
echo Script directory: %~dp0
echo Script full path: %~f0
echo.

REM === Step 1: Check Admin ===
echo [1/5] Checking administrator privileges...
net session >nul 2>&1
if errorlevel 1 (
    echo [FAIL] Need administrator privileges!
    echo.
    echo Right-click install.bat and select "Run as administrator"
    echo.
    pause
    exit /b 1
)
echo [OK] Administrator
echo.

REM === Step 2: Set paths ===
echo [2/5] Setting up paths...
cd /d "%~dp0"
set "INSTALL_DIR=%~dp0"
set "DLL_NAME=CorelDrawAutoIgnoreError.dll"
set "DLL_PATH=%INSTALL_DIR%%DLL_NAME%"

echo Install directory: %INSTALL_DIR%
echo DLL name: %DLL_NAME%
echo Full DLL path: %DLL_PATH%
echo.

REM === DEBUG: List files ===
echo Files in install directory:
dir /b "%INSTALL_DIR%"
echo.

REM === Step 3: Check if DLL exists ===
echo [3/5] Checking for plugin file...

if exist "%DLL_PATH%" (
    echo [OK] Found %DLL_NAME%
    for %%A in ("%DLL_PATH%") do echo Size: %%~zA bytes
) else (
    echo [FAIL] Cannot find %DLL_NAME%
    echo.
    echo Expected location:
    echo %DLL_PATH%
    echo.
    echo Please make sure install.bat and %DLL_NAME% are in the same folder!
    echo.
    pause
    exit /b 1
)
echo.

REM === Step 4: Find RegAsm ===
echo [4/5] Finding RegAsm.exe...

set "REGASM="
if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" (
    set "REGASM=%windir%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe"
    echo [OK] Found 64-bit RegAsm
) else if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe" (
    set "REGASM=%windir%\Microsoft.NET\Framework\v4.0.30319\regasm.exe"
    echo [OK] Found 32-bit RegAsm
) else (
    echo [FAIL] RegAsm.exe not found
    echo.
    echo Please install .NET Framework 4.7.2 or later
    echo Download: https://dotnet.microsoft.com/download/dotnet-framework
    echo.
    pause
    exit /b 1
)
echo.

REM === Step 5: Register COM ===
echo [5/5] Registering COM component...
echo.
echo Command:
echo "%REGASM%" /codebase "%DLL_PATH%"
echo.
echo Please wait...
echo.
echo ----------------------------------------

REM Run RegAsm and save output to temp file
set "TEMP_OUTPUT=%TEMP%\regasm_output.txt"
"%REGASM%" /codebase "%DLL_PATH%" > "%TEMP_OUTPUT%" 2>&1

REM Display the output
type "%TEMP_OUTPUT%"
echo.
echo ----------------------------------------

REM Check if registration was successful
findstr /C:"成功注册" /C:"Types registered successfully" "%TEMP_OUTPUT%" > nul
set REG_RESULT=%errorlevel%

del "%TEMP_OUTPUT%"

if %REG_RESULT% neq 0 (
    echo.
    echo [FAIL] Registration failed
    echo.
    echo Possible solutions:
    echo   - Disable antivirus temporarily
    echo   - Install .NET Framework 4.7.2
    echo   - Re-download the plugin
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] Registration successful!
echo.
echo NOTE: The warning about unsigned assembly is normal and can be ignored.
echo       The plugin will work correctly.
echo.

REM === Create log ===
echo %DLL_PATH% > "%INSTALL_DIR%install.log"
echo %date% %time% >> "%INSTALL_DIR%install.log"

REM === Done ===
echo.
echo ========================================
echo SUCCESS! Installation Complete
echo ========================================
echo.
echo IMPORTANT: How to verify the plugin is working:
echo.
echo 1. Close CorelDRAW if it's currently running
echo.
echo 2. Start CorelDRAW
echo.
echo 3. You should see a GREEN popup message saying:
echo    "Plugin Loaded Successfully!"
echo    "The error dialog auto-ignore feature is now active."
echo.
echo 4. If you DON'T see the popup, the plugin didn't load.
echo    In that case, try manual loading:
echo      a. Tools ^> Options ^> Workspace ^> Automation
echo      b. Click "Load/Unload Add-ins"
echo      c. Add this path: %DLL_PATH%
echo      d. Restart CorelDRAW
echo.
echo 5. To test if it's working:
echo    - Open a CDR file with errors
echo    - The error dialog should close automatically
echo    - You'll see another popup confirming the auto-ignore worked
echo.
echo ========================================
echo.

pause
