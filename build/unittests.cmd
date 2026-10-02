@rem Run all unit tests in Networking-Vega. Used by CDPx pipeline.
setlocal enabledelayedexpansion

set noCodeCoverage=%1
set configuration=%2

if "%noCodeCoverage%" neq "noCodeCoverage" (
	set runSettings=--settings %~dp0\unittests.runsettings
)

if "%configuration%" equ "" (
	set configuration=Release
)

cd /d %~dp0
set repoRoot=%cd%\..

cd %repoRoot%\src

set ut=dotnet test --logger:trx -c %configuration% -r %repoRoot%\TestResults
set TestEnvironment=QTEST

%ut% Common\CommunicationProtocol\unittest\CommunicationProtocolUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Tests\EventSourceValidation\EventSourceValidation.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Backend\HelperTypes\unittest\HelperTypesUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Infrastructure\LogStream\unittests\LogStreamUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Tests\MiscellaneousTests\MiscellaneousTests.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Backend\Core\stress\RingMasterBackendCoreStress.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Backend\Core\unittest\RingMasterBackendCoreUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Backend\Native\unittest\RingMasterBackendNativeUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Common\RingMasterClient\unittest\RingMasterClientUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Common\RingMasterCommon\unittest\RingMasterCommonUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Common\SecureTransport\unittest\SecureTransportUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Infrastructure\ServiceFabric\unittests\ServiceFabricUnitTest.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Tests\RingMasterBVT\RingMasterBVT.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%
%ut% Tests\EndToEndTests\EndToEndTests.csproj %runSettings%
if "%errorlevel%" neq "0" exit /b %errorlevel%

cd %repoRoot%\out\%configuration%-x64
VegaCodeCoverage\Microsoft.Vega.CodeCoverage.exe %repoRoot%\TestResults %repoRoot%\CoverageReport

dotnet tool install dotnet-reportgenerator-globaltool --tool-path tools
tools\reportgenerator -reports:%repoRoot%\CoverageReport\MergedCoverage.xml -targetdir:%repoRoot%\CoverageReport -reporttypes:Cobertura
tools\reportgenerator -reports:%repoRoot%\CoverageReport\MergedCoverage.xml -targetdir:%repoRoot%\CoverageReport -reporttypes:htmlInline

endlocal
