@echo off
chcp 65001 >nul
title MessengerSlayer - Safe cross-platform commit
setlocal EnableExtensions
cd /d "%~dp0.."

set "BRANCH=integration/cross-platform-launchers"

echo.
echo ========================================
echo MessengerSlayer - Safe Commit
echo ========================================
echo.
echo Target branch:
echo   %BRANCH%
echo.
echo Existing Server / Shared / DB files are forbidden.
echo.

where git >nul 2>&1
if errorlevel 1 (
    echo ERROR: Git was not found.
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK was not found.
    pause
    exit /b 1
)

git rev-parse --show-toplevel >nul 2>&1
if errorlevel 1 (
    echo ERROR: This project folder is not a Git repository.
    echo.
    echo Clone the repository first or use the publish bundle.
    pause
    exit /b 1
)

echo [1/6] Fetch...
git fetch origin
if errorlevel 1 goto error

echo.
echo [2/6] Creating/switching branch...
git show-ref --verify --quiet "refs/heads/%BRANCH%"
if not errorlevel 1 (
    git switch "%BRANCH%"
    if errorlevel 1 goto error
) else (
    git switch -c "%BRANCH%"
    if errorlevel 1 goto error
)

echo.
echo [3/6] Verifying changed-file scope...
set "BAD_SCOPE=0"

for /f "delims=" %%F in ('git status --porcelain') do (
    call :CHECK_STATUS_LINE "%%F"
)

if "%BAD_SCOPE%"=="1" (
    echo.
    echo ERROR: Forbidden colleague files are modified.
    echo Nothing will be staged.
    goto error
)

echo Scope PASS.

echo.
echo [4/6] Build Debug + Release...
dotnet restore ".\src\MessengerSlayer.slnx"
if errorlevel 1 goto error

dotnet build ".\src\MessengerSlayer.slnx" -c Debug --no-restore
if errorlevel 1 goto error

dotnet build ".\src\MessengerSlayer.slnx" -c Release --no-restore
if errorlevel 1 goto error

echo.
echo [5/6] Stage allowed files...
git add -- "src/Messenger.Client"
git add -- "docker-compose.yml"
git add -- ".env.example"
git add -- "CROSS_PLATFORM_GUIDE.md"
git add -- "scripts/crossplatform"
git add -- "bats"

set "BAD_STAGE=0"
for /f "delims=" %%F in ('git diff --cached --name-only') do (
    call :CHECK_ALLOWED_PATH "%%F"
)

if "%BAD_STAGE%"=="1" (
    echo ERROR: Forbidden file reached staging.
    git reset
    goto error
)

git diff --cached --quiet
if not errorlevel 1 (
    echo ERROR: No allowed changes to commit.
    goto error
)

git commit -m "feat: add cross-platform one-click launchers"
if errorlevel 1 goto error

echo.
echo [6/6] Push...
git push -u origin "%BRANCH%"
if errorlevel 1 goto error

echo.
echo ========================================
echo SUCCESS
echo ========================================
echo.
echo Branch:
echo   %BRANCH%
echo.
echo Existing colleague Server / Shared / DB files were not staged.
echo.
pause
exit /b 0

:CHECK_STATUS_LINE
set "RAW=%~1"
set "PATHVALUE=%RAW:~3%"
call :CHECK_ALLOWED_PATH "%PATHVALUE%"
if "%BAD_STAGE%"=="1" (
    set "BAD_SCOPE=1"
    set "BAD_STAGE=0"
)
exit /b 0

:CHECK_ALLOWED_PATH
set "PATHVALUE=%~1"

echo %PATHVALUE% | findstr /B /C:"src/Messenger.Client/" >nul
if not errorlevel 1 exit /b 0

echo %PATHVALUE% | findstr /B /C:"scripts/crossplatform/" >nul
if not errorlevel 1 exit /b 0

echo %PATHVALUE% | findstr /B /C:"bats/" >nul
if not errorlevel 1 exit /b 0

if "%PATHVALUE%"=="docker-compose.yml" exit /b 0
if "%PATHVALUE%"==".env.example" exit /b 0
if "%PATHVALUE%"=="CROSS_PLATFORM_GUIDE.md" exit /b 0

echo FORBIDDEN FILE: %PATHVALUE%
set "BAD_STAGE=1"
exit /b 0

:error
echo.
echo ========================================
echo STOPPED
echo ========================================
echo.
echo No force-push was used.
echo Review the error above.
echo.
pause
exit /b 1
