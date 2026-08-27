@echo off
chcp 65001 >nul
title MessengerSlayer - Windows launcher
setlocal EnableExtensions
cd /d "%~dp0.."

echo.
echo ========================================
echo MessengerSlayer - Windows
echo ========================================
echo.

where docker >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker was not found.
    echo Install/start Docker Desktop first.
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK was not found.
    pause
    exit /b 1
)

echo [1/4] Starting SQL Server container...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\crossplatform\db-up.ps1"
if errorlevel 1 goto error

echo.
echo [2/4] Building project...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\crossplatform\build.ps1"
if errorlevel 1 goto error

echo.
echo [3/4] Starting server in a new terminal...
start "MessengerSlayer Server" powershell -NoExit -NoProfile -ExecutionPolicy Bypass -File "%CD%\scripts\crossplatform\run-server.ps1"

echo Waiting for server startup...
timeout /t 4 /nobreak >nul

echo.
echo [4/4] Starting client...
start "MessengerSlayer Client" powershell -NoExit -NoProfile -ExecutionPolicy Bypass -File "%CD%\scripts\crossplatform\run-client.ps1"

echo.
echo ========================================
echo STARTED
echo ========================================
echo.
echo SQL:    Docker container messengerslayer-sql
echo Server: separate PowerShell window
echo Client: separate PowerShell window
echo.
echo To open a second client, run:
echo   powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\run-client.ps1
echo.
pause
exit /b 0

:error
echo.
echo ========================================
echo START FAILED
echo ========================================
echo.
pause
exit /b 1
