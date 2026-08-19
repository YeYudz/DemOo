@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  Copy HybridCLR hot-update DLLs to YooAsset collection folder
::  Usage: double-click to run, or: CopyHotUpdateDlls.bat [Platform]
::  Default platform: StandaloneWindows64
:: ============================================================

set "PROJECT_ROOT=%~dp0"
set "PLATFORM=%~1"
if "%PLATFORM%"=="" set "PLATFORM=StandaloneWindows64"

set "HOTUPDATE_DLL_DIR=%PROJECT_ROOT%HybridCLRData\HotUpdateDlls\%PLATFORM%"
set "AOT_STRIP_DIR=%PROJECT_ROOT%HybridCLRData\AssembliesPostIl2CppStrip\%PLATFORM%"
set "TARGET_DIR=%PROJECT_ROOT%Assets\HotUpdateCodeAssets"

echo ============================================================
echo  [HybridCLR] Copying code DLLs to YooAsset collect folder
echo  Platform: %PLATFORM%
echo ============================================================

if not exist "%TARGET_DIR%" (
    mkdir "%TARGET_DIR%"
    echo [MKDIR] %TARGET_DIR%
)

:: ---------- Step 1: HotUpdate.dll ----------
echo.
echo [STEP 1] Verify and copy HotUpdate hot-update DLL...
if not exist "%HOTUPDATE_DLL_DIR%\HotUpdate.dll" (
    echo     !! ERROR : HotUpdate.dll not found
    echo        Run: HybridCLR ^> CompileDll ^> %PLATFORM%
    exit /b 1
)

:: 直接覆盖，避免源 DLL 不存在时误删上一份可用的 .bytes 文件。
copy /y "%HOTUPDATE_DLL_DIR%\HotUpdate.dll" "%TARGET_DIR%\HotUpdate.dll.bytes" >nul
if errorlevel 1 (
    echo     !! ERROR : Failed to copy HotUpdate.dll
    exit /b 1
)

fc /b "%HOTUPDATE_DLL_DIR%\HotUpdate.dll" "%TARGET_DIR%\HotUpdate.dll.bytes" >nul
if errorlevel 1 (
    echo     !! ERROR : HotUpdate.dll.bytes verification failed
    exit /b 1
)
echo     OK : HotUpdate.dll -^> HotUpdate.dll.bytes (verified)

:: ---------- Step 2: AOT metadata DLLs ----------
echo.
echo [STEP 2] Copy AOT stripped DLLs for metadata supplement...

set "FOUND_AOT=0"

call :CopyAotDll "mscorlib.dll"
call :CopyAotDll "System.dll"
call :CopyAotDll "Assembly-CSharp.dll"
call :CopyAotDll "Shared.dll"

if %FOUND_AOT%==0 (
    echo.
    echo     !! NOTICE : No AOT DLL found in %AOT_STRIP_DIR%
    echo        Option 1: Run HybridCLR ^> Generate ^> StripAOTDll
    echo        Option 2: Build a Standalone Windows Player,
    echo                  Unity will generate stripped AOT DLLs automatically
)

:: ---------- Step 3: Result ----------
echo.
echo [STEP 3] Result listing:
set "COUNT=0"
for %%F in ("%TARGET_DIR%\*.bytes") do (
    echo     - %%~nxF
    set /a COUNT+=1
)
if %COUNT%==0 echo     (empty)

echo.
echo ============================================================
echo  Done. %COUNT% file(s) copied.
echo  1. In Unity: right-click Assets/HotUpdateCodeAssets -^> Reimport
echo  2. Open: YooAsset ^> AssetBundle Builder
echo     Check that CodeGroup collects 5 .bytes files (HotUpdate + 4 AOT metadata)
echo ============================================================

pause
endlocal
exit /b 0

:: ---------- Subroutine ----------
:CopyAotDll
set "DLL_NAME=%~1"
if exist "%AOT_STRIP_DIR%\%DLL_NAME%" (
    copy /y "%AOT_STRIP_DIR%\%DLL_NAME%" "%TARGET_DIR%\%DLL_NAME%.bytes" >nul
    echo     OK : %DLL_NAME% -^> %DLL_NAME%.bytes
    set /a FOUND_AOT+=1
) else (
    echo     SKIP: %DLL_NAME% not found (ignorable if not used)
)
exit /b 0
