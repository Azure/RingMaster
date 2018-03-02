Param
(
[Parameter(Mandatory=$true)]
[String]
$ApplicationPackagePath,

[Parameter(Mandatory=$true)]
[String]
$ApplicationParametersFile,

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
    Remove-ServiceFabricApplication -ApplicationName $applicationName -ForceRemove
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $applicationTypeName -ApplicationTypeVersion $applicationTypeVersion -Force
}
else
{
    Remove-ServiceFabricApplication -ApplicationName $applicationName
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $applicationTypeName -ApplicationTypeVersion $applicationTypeVersion
}
