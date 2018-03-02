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
$Force = $false
)

[xml]$manifest = (Get-Content -Path "$ApplicationPackagePath\ApplicationManifest.xml")
[xml]$parameters = (Get-Content -Path $ApplicationParametersFile)

$applicationTypeName = $manifest.ApplicationManifest.ApplicationTypeName
$applicationTypeVersion = $manifest.ApplicationManifest.ApplicationTypeVersion
$applicationName = $parameters.Application.Name
$applicationParameter = @{}
$parameters.Application.Parameters.Parameter | Foreach-Object { $applicationParameter[$_.Name] = $_.Value }

if ($Force)
{
    Remove-ServiceFabricApplication -ApplicationName $applicationName
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $applicationTypeName -ApplicationTypeVersion $applicationTypeVersion
}

# Copy the application package to the Service Fabric Image Store and register it
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $ApplicationPackagePath -ImageStoreConnectionString $ImageStoreConnectionString -ApplicationPackagePathInImageStore $applicationTypeName -Compress
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $applicationTypeName

# Create a new application in the cluster
New-ServiceFabricApplication -ApplicationName $applicationName -ApplicationTypeName $applicationTypeName -ApplicationTypeVersion $applicationTypeVersion -ApplicationParameter $applicationParameter
