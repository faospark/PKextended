@echo off
setlocal enabledelayedexpansion

echo ========================================
echo PNG to BC3 DDS Converter
echo ========================================
echo.
echo This script will:
echo 1. Convert all PNG files to BC3_UNORM DDS format
echo 2. Delete original PNG files after conversion
echo.
echo Target directory: E:\suikodum\Project Kyaro Build\PKCore\Textures
echo.
pause

echo.
echo Starting conversion...
echo.

REM Counter for tracking progress
set /a total=0
set /a converted=0
set /a failed=0

REM Count total PNG files
for /r "E:\suikodum\Project Kyaro Build\PKCore\Textures" %%f in (*.png) do (
    set /a total+=1
)

echo Found %total% PNG files to convert
echo.

REM Convert each PNG file to DDS
for /r "E:\suikodum\Project Kyaro Build\PKCore\Textures" %%f in (*.png) do (
    set /a converted+=1
    echo [!converted!/%total%] Converting: %%~nxf
    
    REM Run texconv with BC3_UNORM (DXT5) format, output to same directory
    REM Use -- separator to handle paths with spaces correctly
    texconv -f BC3_UNORM -y -o "%%~dpf" -- "%%f" >nul 2>&1
    
    REM Check if conversion was successful
    if exist "%%~dpnf.dds" (
        echo   ^> Success! Deleting PNG...
        del "%%f"
    ) else (
        echo   ^> FAILED! Keeping PNG file.
        set /a failed+=1
    )
    echo.
)

echo ========================================
echo Conversion Complete!
echo ========================================
echo Total files: %total%
echo Successfully converted: %converted%
echo Failed: %failed%
echo.
if %failed% GTR 0 (
    echo Some files failed to convert.
    echo This is usually due to corrupted PNG files.
)
echo.
pause
