@echo off

REM Publish
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
xcopy "%sourcePath%\*" "%publishPath%\wwwroot\" /s /y /i

REM Deleting secrets
if exist "%publishPath%\appCfg.json5" del "%publishPath%\appCfg.json5"

REM Clear temp directory
rd /s /q "%tempDir%" 2>nul
mkdir "%tempDir%"

REM Copy all files to temp
xcopy "%absolutePath%\*" "%tempDir%\TeReoLocalizer\" /s /y /i

REM Create shortcut
powershell -Command "$WS = New-Object -ComObject WScript.Shell; $SC = $WS.CreateShortcut('%tempDir%\TeReoLocalizer.lnk'); $SC.TargetPath = '%tempDir%\TeReoLocalizer\TeReoLocalizer.exe'; $SC.Save()"

cls
echo Creating ZIP file...
powershell -Command "Compress-Archive -Path '%tempDir%\*' -DestinationPath '%distPath%\TeReoLocalizer.zip' -Force"

REM Clean up
rd /s /q "%tempDir%"

REM Get clean path
for /f "delims=" %%i in ('powershell -Command "(Resolve-Path '%distPath%\TeReoLocalizer.zip').Path"') do set "cleanPath=%%i"

echo.
echo Final ZIP location:
echo [93m%cleanPath%[0m
echo.

if not defined AUTOMATION_MODE (
    echo Press any key to close this window...
    pause >nul
)