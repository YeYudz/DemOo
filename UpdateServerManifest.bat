@echo off
echo ============================================================
echo YooAsset Server Manifest Updater
echo Update server manifest after incremental build
echo ============================================================
echo.

cd /d "%~dp0"

set "ROOT=Bundles\StandaloneWindows64"
set "PKG=DefaultPackage"
set "SRC=%ROOT%\%PKG%"

if not exist "%SRC%" (
    echo [ERROR] Package dir not found: %SRC%
    pause
    exit /b 1
)

rem Select the most recently built valid version directory by last-write time.
rem Do not sort by version directory name: 1173 is newer than 990.
set "LATEST="
for /f "delims=" %%D in ('dir /b /ad /o-d "%SRC%"') do (
    if exist "%SRC%\%%D\%PKG%_%%D.bytes" (
        set "LATEST=%%D"
        goto :found
    )
)
:found

if "%LATEST%"=="" (
    echo [ERROR] No valid version directory found in %SRC%
    pause
    exit /b 1
)

echo [INFO] Latest version by build time: %LATEST%
set "VER=%SRC%\%LATEST%"

echo.
echo [STEP 1] Copy DefaultPackage.version and hash to server root...
copy /y "%VER%\%PKG%.version" "%ROOT%\%PKG%.version" >nul
if exist "%VER%\%PKG%.hash" copy /y "%VER%\%PKG%.hash" "%ROOT%\%PKG%.hash" >nul

echo [STEP 2] Copy latest manifest to server root...
copy /y "%VER%\%PKG%_%LATEST%.bytes" "%ROOT%\%PKG%_%LATEST%.bytes" >nul
copy /y "%VER%\%PKG%_%LATEST%.hash" "%ROOT%\%PKG%_%LATEST%.hash" >nul

echo [STEP 3] Sync bundle files to server root...
robocopy "%VER%" "%ROOT%" *.bundle /r:1 /w:0 /nfl /ndl /njh /njs >nul 2>&1
robocopy "%VER%" "%ROOT%" *.bytes /r:1 /w:0 /nfl /ndl /njh /njs >nul 2>&1

echo.
echo [RESULT] Server root files:
echo %ROOT%\
dir /b "%ROOT%\%PKG%.*" 2>nul
echo.
echo %ROOT%\ (latest manifest)
dir /b "%ROOT%\%PKG%_%LATEST%.*" 2>nul
echo.
echo %ROOT%\ (bundle count)
dir /b "%ROOT%\*.bundle" 2>nul | find /c ".bundle" 2>nul
echo bundles

echo.
echo ============================================================
echo DONE. Now run the existing exe to test hot-update!
echo ============================================================
pause
