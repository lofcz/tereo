@echo off

REM Build TeReoLocalizer.Launcher
echo Building TeReoLocalizer.Launcher...
pushd ..\TeReoLocalizer.Launcher
dotnet msbuild -t:Build -p:Configuration=Release
if errorlevel 1 (
    echo Failed to build TeReoLocalizer.Launcher
    exit /b 1
)
copy /Y "bin\Release\net472\TeReoLocalizer.Launcher.exe" "bin\Release\net472\reo.exe"
popd

REM Build TeReoLocalizer.Updater
echo Building TeReoLocalizer.Updater...
pushd ..\TeReoLocalizer.Updater
dotnet msbuild -t:Build -p:Configuration=Release
if errorlevel 1 (
    echo Failed to build TeReoLocalizer.Updater
    exit /b 1
)
copy /Y "bin\Release\TeReoLocalizer.Updater.exe" "bin\Release\updater.exe"
popd

REM Publish
dotnet msbuild -t:GenerateHostPage
dotnet publish -f net8.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true

REM Storing paths
set "publishPath=.\bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish"
set "sourcePath=..\TeReoLocalizer.Shared\wwwroot"
set "absolutePath=%CD%\bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish"
set "tempDir=%TEMP%\TeReoLocalizerDeploy"
set "distPath=%absolutePath%\..\dist"

echo Absolute path to publish: %absolutePath%

REM Making dirs
mkdir "%publishPath%\wwwroot" 2>nul
mkdir "%tempDir%" 2>nul
mkdir "%distPath%" 2>nul

REM Copying assets
echo Copying wwwroot content...
xcopy "%sourcePath%\*" "%publishPath%\wwwroot\" /s /y /i
xcopy "wwwroot\index.g.html" "%publishPath%\wwwroot\" /y

REM Verify wwwroot content
echo Verifying wwwroot content...
dir "%publishPath%\wwwroot"

REM Deleting secrets
if exist "%publishPath%\appCfg.json5" del "%publishPath%\appCfg.json5"

REM Clear temp directory
rd /s /q "%tempDir%" 2>nul
mkdir "%tempDir%"

REM Copy all files to temp
xcopy "%absolutePath%\*" "%tempDir%\TeReoLocalizer\" /s /y /i

REM Copy reo.exe to temp
copy "..\TeReoLocalizer.Launcher\bin\Release\net472\reo.exe" "%tempDir%"
REM Copy updater.exe to temp/TeReoLocalizer
copy "..\TeReoLocalizer.Updater\bin\Release\updater.exe" "%tempDir%\TeReoLocalizer"

if defined AUTOMATION_MODE (
    echo Running in automation mode...
    rd /s /q "%distPath%" 2>nul
    mkdir "%distPath%"
    xcopy "%tempDir%\*" "%distPath%\" /s /y /i
) else (
    echo Creating ZIP file...
    powershell -Command "Compress-Archive -Path '%tempDir%\*' -DestinationPath '%distPath%\TeReoLocalizer.zip' -Force"
    for /f "delims=" %%i in ('powershell -Command "(Resolve-Path '%distPath%\TeReoLocalizer.zip').Path"') do set "cleanPath=%%i"
    echo.
    echo Final ZIP location:
    echo [93m%cleanPath%[0m
)

REM Clean up
rd /s /q "%tempDir%"

echo.

if not defined AUTOMATION_MODE (
    echo Press any key to close this window...
    pause >nul
)