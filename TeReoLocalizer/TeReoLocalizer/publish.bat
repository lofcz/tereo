@echo off

REM Publish
dotnet publish -f net8.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true

REM Nastavení cest
set "publishPath=.\bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish"
set "sourcePath=..\TeReoLocalizer.Shared\wwwroot"

REM Vytvoření adresářů
mkdir "%publishPath%\wwwroot\Content" 2>nul
mkdir "%publishPath%\wwwroot\Scripts" 2>nul

REM Kopírování souborů
xcopy "%sourcePath%\Content\*.css" "%publishPath%\wwwroot\Content\" /s /y
xcopy "%sourcePath%\Content\*.scss" "%publishPath%\wwwroot\Content\" /s /y
xcopy "%sourcePath%\Content\*.map" "%publishPath%\wwwroot\Content\" /s /y
xcopy "%sourcePath%\Scripts\*.js" "%publishPath%\wwwroot\Scripts\" /s /y
xcopy "%sourcePath%\Scripts\*.ts" "%publishPath%\wwwroot\Scripts\" /s /y
xcopy "%sourcePath%\Scripts\*.map" "%publishPath%\wwwroot\Scripts\" /s /y