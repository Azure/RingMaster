@rem Run all unit tests in Networking-Vega. Used by CDPx pipeline.
setlocal enabledelayedexpansion

cd /d %~dp0
cd ..\out

set subdir=%~1
if "%subdir%"=="" set subdir=Release-x64
cd %subdir%

set ut=dotnet vstest --logger:trx
set TestEnvironment=QTEST

%ut% CommunicationProtocolUnitTest\Microsoft.RingMaster.CommunicationProtocolUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% EventSourceValidation\Microsoft.RingMaster.Test.EventSourceValidation.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% HelperTypesUnitTest\Microsoft.RingMaster.HelperTypesUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% LogStreamUnitTest\Microsoft.RingMaster.LogStreamUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% MiscellaneousTests\MiscellaneousTests.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% RingMasterBackendCoreStress\Microsoft.RingMaster.Backend.CoreStress.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% RingMasterBackendCoreUnitTest\Microsoft.RingMaster.Backend.CoreUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% RingMasterBackendNativeUnitTest\Microsoft.RingMaster.Backend.SortedDictExtUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% RingMasterClientUnitTest\Microsoft.RingMaster.ClientUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% RingMasterCommonUnitTest\Microsoft.RingMaster.CommonUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% SecureTransportUnitTest\Microsoft.RingMaster.SecureTransportUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% ServiceFabricUnitTest\Microsoft.RingMaster.ServiceFabricUnitTest.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%

%ut% RingMasterBVT\Microsoft.RingMaster.Test.BVT.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% EndToEndTests\Microsoft.RingMaster.Test.EndToEnd.dll
if "%errorlevel%" neq "0" exit /b %errorlevel%

endlocal
