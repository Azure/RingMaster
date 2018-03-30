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

function GetImageStoreConnectionString
{
    Write-Verbose "GetImageStoreConnectionString"
    [xml] $clusterManifest = Get-ServiceFabricClusterManifest
    $managementSection = $clusterManifest.ClusterManifest.FabricSettings.Section | ? { $_.Name -eq "Management" }
    $connectionString = $managementSection.Parameter | ? { $_.Name -eq "ImageStoreConnectionString" } | Select-Object -Expand Value
    Write-Verbose "Connection string: [$connectionString]"

    return $connectionString
}

$ImageStoreConnectionString = GetImageStoreConnectionString

if ($Force) {
    Remove-ServiceFabricApplication -ApplicationName $applicationName -Force
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $applicationTypeName `
        -ApplicationTypeVersion $applicationTypeVersion -Force
}

# Copy the application package to the Service Fabric Image Store and register it
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $ApplicationPackagePath `
    -ImageStoreConnectionString $ImageStoreConnectionString `
    -ApplicationPackagePathInImageStore $applicationTypeName
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $applicationTypeName

# Create a new application in the cluster
New-ServiceFabricApplication -ApplicationName $applicationName `
    -ApplicationTypeName $applicationTypeName `
    -ApplicationTypeVersion $applicationTypeVersion `
    -ApplicationParameter $applicationParameter
