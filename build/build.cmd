@rem Build all projects without create nuget packages in Networking-Vega. Used by CDPx pipeline.
setlocal enabledelayedexpansion

@rem Starting at the enlistment root
cd /d %~dp0
cd ..

@rem Check if MSBuild is in the current PATH. If not, bootstrap VS2019 Developer Command Prompt
where msbuild
if %errorlevel% neq 0 (
    echo ### Bootstrap VS2019 dev environment
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 -test
)

where dotnet
if %errorlevel% neq 0 (
    PowerShell -ExecutionPolicy Bypass -NoProfile -NoLogo build\install-dotnet.ps1
) else (
    dotnet --info
    dotnet --info | findstr 6.0.100
    if %errorlevel% neq 0 (
        PowerShell -ExecutionPolicy Bypass -NoProfile -NoLogo build\install-dotnet.ps1
    )
    dotnet --info
)

call :restore
if %errorlevel% neq 0 (
    exit /b 1
)

call :build
if %errorlevel% neq 0 (
    exit /b 1
)

goto :finished

:restore

echo ### Restoring nuget packages packages.config
build\Local\NuGet\nuget.exe restore src\packages.config -Verbosity detailed -NonInteractive

if %errorlevel% neq 0 (
    echo **FAILED to restore nuget packages in packages.config**
    exit /b 1
)

echo ### Restoring nuget packages in package references
msbuild /t:restore dirs.proj

if %errorlevel% neq 0 (
    echo **FAILED to restore nuget packages in package references**
    exit /b 1
)

exit /b 0

:build

echo ### Build all projects
msbuild /m /p:Configuration=Release /p:Platform=x64 /fl /clp:Summary;ForceNoAlign;Verbosity=minimal src\dirs.proj

if %errorlevel% neq 0 (
    echo **FAILED to build projects**
    exit /b 1
)

exit /b 0

:finished

exit /b 0

endlocal
