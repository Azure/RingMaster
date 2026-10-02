@setlocal
@rem wait for the service to start
@rem timeout /t 300

set "SFBIN=C:\Program Files\Microsoft Service Fabric\bin\Fabric\Fabric.Code"
if exist "%SFBIN%\FabricClient.dll" (
  set "PATH=%SFBIN%;%PATH%"
)

set PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%
@rem Solve the hostfxr.dll not found issue
set DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet
dotnet.exe %1 %2 /logger:trx /Platform:x64 /Framework:".NETCoreApp,Version=v8.0"

@endlocal
