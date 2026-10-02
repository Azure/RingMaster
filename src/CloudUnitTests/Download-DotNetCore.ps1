<#
.SYNOPSIS
    Downloads and installs dotnet core runtime 2.0.5 for Windows 64-bit platform for testing purpose

.NOTES
    This requires outbound internet connection to Microsoft public download website. It will write about 85 MB of data
    to installation directory.
#>
[CmdletBinding()]
param(
    [string] $Version = "8.0.419"
)

Set-StrictMode -Version latest

$AllProtocols = [System.Net.SecurityProtocolType]'Ssl3,Tls,Tls11,Tls12'
[System.Net.ServicePointManager]::SecurityProtocol = $AllProtocols

Invoke-WebRequest -Uri "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"
.\dotnet-install.ps1 -Version $Version
# It is installed to default path. Need to add it in front in PATH for cmd "dotnet vstest" to locate it
$path = [System.Environment]::GetEnvironmentVariable('Path', [System.EnvironmentVariableTarget]::Machine)
[System.Environment]::SetEnvironmentVariable('Path', "$env:LOCALAPPDATA\Microsoft\dotnet\;" + $path, [System.EnvironmentVariableTarget]::Machine)

$dotnet = "dotnet.exe"
md mstest
cd mstest
& $dotnet new mstest
& $dotnet test

$runtimeconfig = Get-ChildItem -Recurse "*.runtimeconfig.dev.json"
$runtimeconfig = Get-Content $runtimeconfig
cd ..

Get-ChildItem -Recurse *.runtimeconfig.dev.json | % {
    Out-File -encoding UTF8 -Force -FilePath $_.FullName -InputObject $runtimeconfig
    $_
}

$proj = '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MSTest.TestAdapter" Version="2.2.8" />
    <PackageReference Include="MSTest.TestFramework" Version="2.2.8" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="6.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="6.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
    <PackageReference Include="Microsoft.ServiceFabric" Version="8.0.521" />
    <PackageReference Include="Microsoft.ServiceFabric.Data" Version="5.0.521" />
    <PackageReference Include="Microsoft.ServiceFabric.Data.Extensions" Version="5.0.521" />
    <PackageReference Include="Microsoft.ServiceFabric.Data.Interfaces" Version="5.0.521" />
    <PackageReference Include="Microsoft.ServiceFabric.Diagnostics.Internal" Version="5.0.521" />
    <PackageReference Include="Microsoft.ServiceFabric.Services" Version="5.0.521" />
    <PackageReference Include="Microsoft.ServiceFabric.Services.Wcf" Version="5.0.521" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
</Project>'
$proj | Out-File -Encoding ascii .\restore.proj

& $dotnet restore .\restore.proj
