@echo off
setlocal

rem Builds ScreenSleeper in Release. Pass "debug" for a Debug build.
rem The NuGet source is given explicitly because this machine has a dead local
rem source configured (NUGET1301), and we build the .csproj rather than the .sln
rem because -o on a solution emits NETSDK1194.

set CONFIG=Release
if /i "%~1"=="debug" set CONFIG=Debug

set PROJECT=%~dp0SleepScreenWPF\ScreenSleeper.csproj
set OUTDIR=%~dp0SleepScreenWPF\bin\%CONFIG%\net8.0-windows

tasklist /fi "imagename eq ScreenSleeper.exe" 2>nul | find /i "ScreenSleeper.exe" >nul
if not errorlevel 1 (
    echo ScreenSleeper is running and holds a lock on the exe.
    choice /m "Close it and keep building"
    if errorlevel 2 goto :cancelled
    taskkill /im ScreenSleeper.exe /f >nul
    rem Give Windows a moment to release the file handle.
    ping -n 2 127.0.0.1 >nul
)

echo Building %CONFIG%...
dotnet build "%PROJECT%" -c %CONFIG% --source https://api.nuget.org/v3/index.json -o "%OUTDIR%"
if errorlevel 1 goto :failed

echo.
echo Built: %OUTDIR%\ScreenSleeper.exe
exit /b 0

:cancelled
echo Build cancelled.
exit /b 1

:failed
echo.
echo Build failed.
exit /b 1
