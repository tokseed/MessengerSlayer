@echo off
setlocal

set "ROOT=%~dp0src"
set "SOLUTION=%ROOT%\MessengerSlayer.slnx"

echo Restoring NuGet packages...
dotnet restore "%SOLUTION%"
if errorlevel 1 exit /b 1

echo Building MessengerSlayer...
dotnet build "%SOLUTION%" --no-restore -m:1 -p:UsedAvaloniaProducts=
if errorlevel 1 exit /b 1

if /I "%~1"=="server" goto server
if /I "%~1"=="client" goto client

echo Starting server in a new window...
start "Messenger Server" cmd /k "cd /d "%ROOT%" && dotnet run --project Messenger.Server\Messenger.Server.csproj --no-build"
timeout /t 3 /nobreak >nul
goto client

:server
dotnet run --project "%ROOT%\Messenger.Server\Messenger.Server.csproj" --no-build
exit /b %errorlevel%

:client
dotnet run --project "%ROOT%\Messenger.Client\Messenger.Client.csproj" --no-build
exit /b %errorlevel%
