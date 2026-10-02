@rem Setup script for cloud test

setlocal

cd /d %~dp0
cd ..

PowerShell -Command .\CloudUnitTests\RunSetup.ps1 -Action UndeployAndCleanup

endlocal