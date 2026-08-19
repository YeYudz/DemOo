@echo off
chcp 65001 >nul
echo ============================================
echo   Unity 编译缓存清理脚本
echo ============================================
echo.

echo [1/3] 正在关闭 Unity...
taskkill /F /IM Unity.exe 2>nul
timeout /t 3 /nobreak >nul

echo [2/3] 正在清理编译缓存...
rmdir /s /q "e:\Unity\Project\DemOo\Library\ScriptAssemblies" 2>nul
rmdir /s /q "e:\Unity\Project\DemOo\Library\Bee" 2>nul
rmdir /s /q "e:\Unity\Project\DemOo\Library\Artifacts" 2>nul
rmdir /s /q "e:\Unity\Project\DemOo\Library\BuildPlayerData" 2>nul
rmdir /s /q "e:\Unity\Project\DemOo\Library\BuildinData" 2>nul
rmdir /s /q "e:\Unity\Project\DemOo\Library\ShaderCache" 2>nul
rmdir /s /q "e:\Unity\Project\DemOo\Library\PackageCache" 2>nul
del /q "e:\Unity\Project\DemOo\Library\UnityLockfile" 2>nul
del /q "e:\Unity\Project\DemOo\Library\LastSceneManagerSetup.txt" 2>nul
del /q "e:\Unity\Project\DemOo\Library\CurrentLayout-default.dwlt" 2>nul

echo [3/3] 完成！正在启动 Unity...
echo.
echo 请等待 Unity 完全启动后，在 Editor 中：
echo   菜单: Assets - Refresh  (或 Ctrl+R)
echo   等待编译完成
echo.
pause
