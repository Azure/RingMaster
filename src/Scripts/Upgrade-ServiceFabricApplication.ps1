Param
(
[Parameter(Mandatory=$true)]
[String]
$ApplicationPackagePath,

[Parameter(Mandatory=$true)]
[String]
$ApplicationParametersFile,

[String]
$ImageStoreConnectionString = "fabric:ImageStore",

[Switch]
$UnMonitored = $false
)

[xml]$manifest = (Get-Content -Path "$ApplicationPackagePath\ApplicationManifest.xml")
[xml]$parameters = (Get-Content -Path $ApplicationParametersFile)

$applicationTypeName = $manifest.ApplicationManifest.ApplicationTypeName
$applicationTypeVersion = $manifest.ApplicationManifest.ApplicationTypeVersion
$applicationName = $parameters.Application.Name
$applicationParameter = @{}
$parameters.Application.Parameters.Parameter | Foreach-Object { $applicationParameter[$_.Name] = $_.Value }

# Copy the application package to the Service Fabric Image Store and register it
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $ApplicationPackagePath -ImageStoreConnectionString $ImageStoreConnectionString -ApplicationPackagePathInImageStore $applicationTypeName -Compress
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $applicationTypeName


if ($UnMonitored)
{
  Start-ServiceFabricApplicationUpgrade -UnmonitoredAuto -ApplicationName $applicationName -ApplicationTypeVersion $applicationTypeVersion -ApplicationParameter $applicationParameter
}
else
{
  Start-ServiceFabricApplicationUpgrade -Monitored -ApplicationName $applicationName -ApplicationTypeVersion $applicationTypeVersion -ApplicationParameter $applicationParameter -HealthCheckStableDurationSec 60 -UpgradeDomainTimeoutSec 1200 -UpgradeTimeout 3000  -FailureAction Rollback
}
