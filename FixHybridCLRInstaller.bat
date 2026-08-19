@echo off
chcp 65001 >nul
echo ============================================
echo   HybridCLR Installer 手动修复脚本
echo ============================================
echo.

set "HCDATA=e:\Unity\Project\DemOo\HybridCLRData"
set "PLATFORMDIR=LocalIl2CppData-StandaloneWindows64"

echo [1/4] 关闭 Unity 建议...
echo.

echo [2/4] 合并 hybridclr 到 il2cpp_plus...
if exist "%HCDATA%\il2cpp_plus_repo\libil2cpp\hybridclr" (
    echo   hybridclr 已存在，跳过合并
) else (
    if exist "%HCDATA%\hybridclr_repo\hybridclr" (
        echo   正在复制 hybridclr 到 il2cpp_plus...
        xcopy "%HCDATA%\hybridclr_repo\hybridclr" "%HCDATA%\il2cpp_plus_repo\libil2cpp\hybridclr\" /E /I /Q /Y
        echo   合并完成
    ) else (
        echo   错误: hybridclr_repo\hybridclr 不存在
    )
)

echo.
echo [3/4] 创建 LocalIl2CppData 目录...
if not exist "%HCDATA%\%PLATFORMDIR%\il2cpp\libil2cpp\hybridclr" (
    echo   正在复制 il2cpp_plus 到 LocalIl2CppData...
    xcopy "%HCDATA%\il2cpp_plus_repo\libil2cpp" "%HCDATA%\%PLATFORMDIR%\il2cpp\libil2cpp\" /E /I /Q /Y
    echo   复制完成
) else (
    echo   LocalIl2CppData 已存在，跳过
)

echo.
echo [4/4] 创建 HotUpdateDlls 和 AssembliesPostIl2CppStrip 目录...
if not exist "%HCDATA%\HotUpdateDlls\StandaloneWindows64" mkdir "%HCDATA%\HotUpdateDlls\StandaloneWindows64"
if not exist "%HCDATA%\AssembliesPostIl2CppStrip\StandaloneWindows64" mkdir "%HCDATA%\AssembliesPostIl2CppStrip\StandaloneWindows64"

echo.
echo ============================================
echo   修复完成！
echo ============================================
echo.
echo 现在请在 Unity 中：
echo   菜单: HybridCLR - Installer - 检查是否已安装
echo   如果仍报错，可跳过 Installer，直接使用:
echo   菜单: HybridCLR - Generate - All
echo.
pause
