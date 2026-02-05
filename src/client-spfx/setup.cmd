@echo off
REM Development Environment Setup Script for Windows
REM This script helps set up the SharePoint Framework development environment

echo 🚀 SharePoint External User Manager - Setup Script
echo ==================================================

REM Check if Node.js is installed
node --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Node.js is not installed. Please install Node.js 16.x or 18.x
    echo    Download from: https://nodejs.org/
    pause
    exit /b 1
)

REM Get Node.js version
for /f "tokens=*" %%i in ('node --version') do set NODE_VERSION=%%i
echo 📋 Current Node.js version: %NODE_VERSION%

REM Extract major version (remove 'v' prefix and get first part)
set "VERSION_NUM=%NODE_VERSION:~1%"
for /f "tokens=1 delims=." %%a in ("%VERSION_NUM%") do set MAJOR_VERSION=%%a

REM Check version compatibility
if %MAJOR_VERSION% LSS 16 (
    goto :version_warning
)
if %MAJOR_VERSION% GTR 18 (
    goto :version_warning
)
goto :install_deps

:version_warning
echo ⚠️  Warning: Node.js version %NODE_VERSION% may not be compatible
echo    SharePoint Framework 1.18.2 requires Node.js 16.x or 18.x
echo    Consider using a version manager:
echo    - Windows: nvm-windows
echo.
echo    Install compatible version:
echo    nvm install 18.17.1
echo    nvm use 18.17.1
echo.
set /p continue="Continue anyway? (y/N): "
if /i "%continue%" NEQ "y" exit /b 1

:install_deps
echo 📦 Installing dependencies...
call npm install

if %ERRORLEVEL% EQU 0 (
    echo ✅ Dependencies installed successfully!
) else (
    echo ❌ Failed to install dependencies
    echo    This might be due to Node.js version compatibility
    echo    Please ensure you're using Node.js 16.x or 18.x
    pause
    exit /b 1
)

echo.
echo 🎉 Setup complete! You can now:
echo    • Run 'npm run serve' to start development server
echo    • Run 'npm run build' to build the solution
echo    • Run 'npm run package-solution' to create deployment package
echo.
echo 📖 For detailed development guide, see DEVELOPER_GUIDE.md
pause