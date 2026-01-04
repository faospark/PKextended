@echo off
echo Testing texconv...
echo.

REM Test if texconv is accessible
where texconv.exe
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: texconv.exe not found in PATH!
    echo.
    echo Please either:
    echo 1. Add texconv.exe location to your PATH environment variable
    echo 2. Copy texconv.exe to this directory: d:\Appz\PKCore\
    echo 3. Copy texconv.exe to: E:\suikodum\Project Kyaro Build\PKCore\Textures\
    echo.
    pause
    exit /b 1
)

echo.
echo texconv.exe found! Testing conversion on first PNG file...
echo.

REM Find first PNG file
for /r "E:\suikodum\Project Kyaro Build\PKCore\Textures" %%f in (*.png) do (
    echo Testing with: %%~nxf
    echo Full path: %%f
    echo Output dir: %%~dpf
    echo.
    
    REM Try conversion with verbose output
    texconv -f BC3_UNORM -y -o "%%~dpf" -- "%%f"
    
    echo.
    if exist "%%~dpnf.dds" (
        echo SUCCESS! DDS file created.
        echo You can now run the full conversion script.
    ) else (
        echo FAILED! Check the error message above.
    )
    
    pause
    goto :end
)

:end
