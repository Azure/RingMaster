@rem Generate the C# code for *.proto files

setlocal

cd /d %~dp0

set TOOLS_PATH=%HOMEDRIVE%%HOMEPATH%\.nuget\packages\grpc.tools\1.12.0\tools\windows_x64

%TOOLS_PATH%\protoc.exe -I . --csharp_out . --grpc_out . .\Protos\DistTestCommonProto.proto --plugin=protoc-gen-grpc=%TOOLS_PATH%\grpc_csharp_plugin.exe

%TOOLS_PATH%\protoc.exe -I .\Protos --csharp_out . --grpc_out . .\Protos\DistributedJobControllerProto.proto --plugin=protoc-gen-grpc=%TOOLS_PATH%\grpc_csharp_plugin.exe

%TOOLS_PATH%\protoc.exe -I .\Protos --csharp_out . --grpc_out . .\Protos\JobRunnerProto.proto --plugin=protoc-gen-grpc=%TOOLS_PATH%\grpc_csharp_plugin.exe

endlocal