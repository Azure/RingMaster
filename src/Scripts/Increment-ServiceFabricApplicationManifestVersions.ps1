<#
.SYNOPSIS
    Increments Service Fabric manifest files from the root of a Service Fabric application.

.DESCRIPTION
    This script takes a root directory of a Service Fabric application and increments all package and manifest versions
    to allow for service fabric to be able to detect and deploy the new changes to the application. This is intended to
    be run in the source root directory of the Service Fabric application, to allow for the manifest version changes to
    be tracked in source control. The user will need to merge the changes to the manifest versions in their source
    control.

.PARAMETER AppPackagePath
    Specifies the directory where the Service Fabric application package is stored.

.PARAMETER AppTypeName
    Specifies the Service Fabric application type name of the desired application to have the manifests updated. If not
    specified, use the type name in the application manifest.

.EXAMPLE
    .\IncrementManifestVersions.ps1 -appFilePath C:\onebranch\SRE\Main\src\Microsoft\Azure\ServiceFabric\test\SF-MAtemp\SF-MAtemp\MonitoringServiceWithMA -appTypeName FabricMonitoringServiceApplication

.LINK
    https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-application-upgrade-tutorial
    https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-application-upgrade-tutorial-powershell
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $True)]
    [ValidateNotNullOrEmpty()]
    [string] $AppPackagePath,

    [Parameter(Mandatory = $False)]
    [ValidateNotNullOrEmpty()]
    [string] $AppTypeName,

    [switch] $Force
)

function Increment-Version
{
    param(
        [Parameter(Mandatory=$True)]
        [string] $OldVersion
    )

    $old = [Version] $OldVersion
    $new = New-Object System.Version($old.Major, $old.Minor, $old.Build, ($old.Revision + 1))
    return $new.ToString()
}

function Increment-ServiceManifestVersion
{
    param(
        [Parameter(Mandatory=$True)]
        [string] $ServiceManifestPath
    )

    [xml] $serviceManifest = Get-Content $ServiceManifestPath
    $manifest = $serviceManifest.ServiceManifest
    $manifest.Version = Increment-Version -oldVersion $manifest.Version

    $serviceManifest.Save($ServiceManifestPath)

    Write-Verbose "Service manifest $($manifest.Name) version incremented to $($manifest.Version)"
    return $manifest.Version
}

function Increment-ApplicationManifestVersion
{
    param(
        [Parameter(Mandatory=$True)]
        [string] $AppPath,

        [Parameter(Mandatory=$False)]
        [string] $AppTypeName
    )

    Write-Output "Incrementing..."
    $appManifestPaths = Get-ChildItem $AppPath -Recurse -Filter "ApplicationManifest.xml"

    foreach ($manifestPath in $appManifestPaths) {
        [xml] $appManifestXml = Get-Content $manifestPath.FullName
        $appManifest = $appManifestXml.ApplicationManifest
        
        Write-Output "ManifestPath: $manifestPath Version $($appManifest.ApplicationTypeVersion)"
        if ((-not [string]::IsNullOrEmpty($AppTypeName)) -and $appManifest.ApplicationTypeName -ne $AppTypeName) {
            Write-Output "Skipping $($appManifest.ApplicationTypeName) location:$($manifestPath.FullName) due to not being correct application manifest to edit."
        }
        elseif (-not $Force -and ($manifestPath.FullName -like '*\bin\*' -or $manifestPath.FullName -like '*\out\*')) {
            Write-Output "Skipping $($appManifest.ApplicationTypeName) location:$($manifestPath.FullName) due to path containing '*\bin\*' or '*\out\*'"
        }
        else{
            $svcManifests = Get-ChildItem $AppPath -Recurse -Filter "ServiceManifest.xml"
            
            # update each package in service manifest
            $appManifest.ServiceManifestImport | foreach {
                foreach($manifest in $svcManifests){
                    [xml] $svcManifest = Get-Content $manifest.FullName
                    if ($svcManifest.ServiceManifest.Name -eq $_.ServiceManifestRef.ServiceManifestName) {
                        Increment-PackageVersion -manifestFilePath $manifest.FullName
                        $_.ServiceManifestRef.ServiceManifestVersion = Increment-ServiceManifestVersion -serviceManifestPath $manifest.FullName
                    }
                }
            }

            $appManifest.ApplicationTypeVersion = Increment-Version -oldVersion $appManifest.ApplicationTypeVersion
            $appManifestXml.Save($manifestPath.FullName)
        
            Write-Verbose "Application manifest version is now $($appManifest.ApplicationTypeVersion) located here: $($manifestPath.FullName)"
            Write-Output "Completed"
        }        
    }
}

function Increment-PackageVersion
{
    param(
        [Parameter(Mandatory=$True)]
        [string] $manifestFilePath
    )

    [xml] $svcManifest = Get-Content $manifestFilePath
    $manifest = $svcManifest.ServiceManifest

    # increment package version of package
    if ($manifest | Get-Member -Name CodePackage){ 
        $manifest.CodePackage | foreach { 
            $_.Version = Increment-Version -oldVersion $_.Version
            Write-Verbose "$($_.Name) version is $($_.Version)"
        }
    }

    if ($manifest | Get-Member -Name ConfigPackage) { 
        $manifest.ConfigPackage | foreach { 
            $_.Version = Increment-Version -oldVersion $_.Version
            Write-Verbose "$($_.Name) version is $($_.Version)"
        }
    }

    if ($manifest | Get-Member -Name DataPackage) {
        $manifest.DataPackage | foreach { 
            $_.Version = Increment-Version -oldVersion $_.Version
            Write-Verbose "$($_.Name) version is $($_.Version)"
        }
    }

    $svcManifest.Save($manifestFilePath)
}

Increment-ApplicationManifestVersion -appPath $AppPackagePath -appTypeName $AppTypeName

# vim: textwidth=120:expandtab
