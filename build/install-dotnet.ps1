$AllProtocols = [System.Net.SecurityProtocolType]'Ssl3,Tls,Tls11,Tls12'
[System.Net.ServicePointManager]::SecurityProtocol = $AllProtocols

Invoke-WebRequest -Uri "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"
.\dotnet-install.ps1 -Version '6.0.100'

$path = [System.Environment]::GetEnvironmentVariable('Path', [System.EnvironmentVariableTarget]::Machine)
[System.Environment]::SetEnvironmentVariable('Path', $path + ";C:\Users\$env:USERNAME\AppData\Local\Microsoft\dotnet\" ,[System.EnvironmentVariableTarget]::Machine)

exit 0
