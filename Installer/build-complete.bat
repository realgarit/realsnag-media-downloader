@echo off
setlocal enabledelayedexpansion

echo ========================================
echo RealSnag Media Downloader - Build Script
echo ========================================
echo.



REM Clean previous builds
echo Cleaning previous builds...
if exist "..\publish" (
    echo Removing publish directory...
    rmdir /s /q "..\publish"
)
if exist "*.msi" (
    echo Removing old MSI files...
    del "*.msi"
)

echo.
echo.
echo Step 1: Preparing installer assets...
echo Converting PNG to ICO for application and product icons...
powershell -ExecutionPolicy Bypass -File "convert-png-to-ico.ps1"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to convert PNG to ICO!
    pause
    exit /b 1
)
echo ICO file created successfully.

echo.
echo Step 2: Building and publishing application...
echo Command: dotnet publish -c Release -r win-x64 --self-contained true -o publish
echo.
cd ..
dotnet publish -c Release -r win-x64 --self-contained true -o publish
cd Installer

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Failed to publish application!
    echo Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Step 3: Copying ICO file to publish directory...
copy "realsnag-media-downloader.ico" "..\publish\realsnag-media-downloader.ico" >nul 2>&1

echo.
echo Step 4: Building MSI installer...

REM Check if WiX is available
wix --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: WiX Toolset not found in PATH!
    echo.
    echo Please install WiX Toolset v4.0 or later:
    echo 1. Download from: https://github.com/wixtoolset/wix4/releases
    echo 2. Add WiX to your PATH environment variable
    echo 3. Or use: dotnet tool install --global wix
    echo.
    pause
    exit /b 1
)

echo Building MSI with WiX...
wix build Product.wxs -o "RealSnag-Media-Downloader-v1.1.1.msi" -define SourceDir="..\publish"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Failed to build MSI installer!
    echo Please check the WiX error messages above.
    pause
    exit /b 1
)

echo.
echo ========================================
echo BUILD COMPLETED SUCCESSFULLY!
echo ========================================
echo.
echo Application published to: ..\publish\
echo MSI installer created: RealSnag-Media-Downloader-v1.1.1.msi
echo.
echo File sizes:
if exist "..\publish\realsnag-media-downloader.exe" (
    for %%A in ("..\publish\realsnag-media-downloader.exe") do echo   Application: %%~zA bytes
)
if exist "RealSnag-Media-Downloader-v1.1.1.msi" (
    for %%A in ("RealSnag-Media-Downloader-v1.1.1.msi") do echo   Installer: %%~zA bytes
)
echo.
echo You can now distribute the MSI file to users.
echo.
pause