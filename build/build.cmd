@rem Build all projects in Networking-Vega. Used by CDPx pipeline.
setlocal enabledelayedexpansion

@rem Starting at the enlistment root
cd /d %~dp0
cd ..

@rem Check if MSBuild is in the current PATH. If not, bootstrap VS2017 Developer Command Prompt
where msbuild
if %errorlevel% neq 0 (
    echo ### Bootstrap VS2017 dev environment
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 -winsdk=10.0.16299.0
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 -winsdk=10.0.16299.0 -test
)

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

:build

echo ### Build all projects
msbuild /m /p:Configuration=Release /p:Platform=x64 /fl /clp:Summary;ForceNoAlign;Verbosity=minimal dirs.proj

if %errorlevel% neq 0 (
    echo **FAILED to build projects**
    exit /b 1
)

goto :finished

:usage

@echo Uage: %~0 [Command]
@echo Command:
@echo     retore     - restore nuget packages
@echo     build      - build all projects

:finished

exit /b 0

endlocal
