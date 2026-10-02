@rem Create nuget packages for all projects in Networking-Vega. Used by CDPx pipeline.
setlocal enabledelayedexpansion

@rem Starting at the enlistment root
cd /d %~dp0
cd ..

@rem Check if MSBuild is in the current PATH. If not, bootstrap VS2019 Developer Command Prompt
where msbuild
if %errorlevel% neq 0 (
    echo ### Bootstrap VS2019 dev environment
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 -winsdk=10.0.16299.0
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 -winsdk=10.0.16299.0 -test
)

echo ### Create nuget packages for all projects
msbuild /m /p:Configuration=Release /p:Platform=x64 /p:CdpxPostSigning=true /p:BuildProjectReferences=false /fl /clp:Summary;ForceNoAlign;Verbosity=detailed src\PackageDefinitions\dirs.proj

if %errorlevel% neq 0 (
    echo **FAILED to create nuget packages for projects**
    exit /b 1
)

exit /b 0

endlocal
