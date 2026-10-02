setlocal EnableExtensions
cd /d %~dp0
cd ..

set "SFBIN=C:\Program Files\Microsoft Service Fabric\bin\Fabric\Fabric.Code"

PowerShell -ExecutionPolicy Bypass -File .\CloudUnitTests\InstallVcredist.ps1
if errorlevel 1 exit /b 1

if not exist "%SFBIN%\FabricClient.dll" (
  PowerShell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference='Stop';" ^
    "$cab=(Get-ChildItem 'ServiceFabric.XCopyPackage\*.cab' | Select-Object -First 1).FullName;" ^
    "if (-not $cab) { throw 'No Service Fabric CAB found in ServiceFabric.XCopyPackage' }" ^
    "& 'ServiceFabric.XCopyPackage\InstallFabric.ps1' -FabricRuntimePackagePath $cab -AcceptEULA"
  if errorlevel 1 exit /b 1
)

if not exist "%SFBIN%\FabricClient.dll" (
  echo FabricClient.dll is still missing after install
  exit /b 1
)

rem Diagnostic only, do not fail setup here
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-Service FabricHostSvc -ErrorAction SilentlyContinue | Format-Table Status,Name,StartType -Auto"

rem Keep this aligned with global.json so dotnet vstest resolves the expected SDK.
PowerShell -ExecutionPolicy Bypass .\CloudUnitTests\Download-DotNetCore.ps1 -Version "8.0.419"
if errorlevel 1 exit /b 1

PowerShell -ExecutionPolicy Bypass .\CloudUnitTests\RunSetup.ps1 -Action SetupAndDeploy
if errorlevel 1 exit /b 1

endlocal
