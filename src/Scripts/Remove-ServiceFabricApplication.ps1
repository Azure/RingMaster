[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [ValidateScript({Test-Path -PathType Container $_})]
    [String] $ApplicationPackagePath,

    [Parameter(Mandatory=$true)]
    [ValidateScript({Test-Path -PathType Leaf $_})]
    [String] $ApplicationParametersFile,

    [Switch] $Force = $false
)

Set-StrictMode -Version latest

[xml]$manifest = (Get-Content -Path "$ApplicationPackagePath\ApplicationManifest.xml")
[xml]$parameters = (Get-Content -Path $ApplicationParametersFile)

$applicationTypeName = $manifest.ApplicationManifest.ApplicationTypeName
$applicationTypeVersion = $manifest.ApplicationManifest.ApplicationTypeVersion
$applicationName = $parameters.Application.Name
$applicationParameter = @{}
$parameters.Application.Parameters.Parameter | Foreach-Object { $applicationParameter[$_.Name] = $_.Value }

[HashTable]$connParams = @{}
$connParams.Add("ConnectionEndpoint", "localhost:19000")
$connParams.Add("TimeoutSec", 10)
$connParams.Add("WarningAction", 'SilentlyContinue')

while ($true) {
    try {
        $testWarnings = @()
        $isConnSuccesfull = Test-ServiceFabricClusterConnection -TimeoutSec 5 -WarningAction SilentlyContinue -WarningVariable testWarnings
        if ($isConnSuccesfull -and ($testWarnings.Count -eq 0)) {
            Write-Host "Local Cluster ready status: 100% completed."
            break
        }
    }
    catch [System.NullReferenceException] {
        Write-Host -ForegroundColor Cyan "$([DateTime]::Now) Connecting to local cluster"
        Connect-ServiceFabricCluster @connParams
    }
    catch [System.Exception] {}
}

if ($Force) {
    Remove-ServiceFabricApplication -ApplicationName $applicationName -Force
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $applicationTypeName `
        -ApplicationTypeVersion $applicationTypeVersion -Force
}
else {
    Remove-ServiceFabricApplication -ApplicationName $applicationName
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $applicationTypeName `
        -ApplicationTypeVersion $applicationTypeVersion
}
