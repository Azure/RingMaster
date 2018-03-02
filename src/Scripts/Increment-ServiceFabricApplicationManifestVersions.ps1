<#
  .SYNOPSIS
      Increments Service Fabric manifest files from the root of a Service Fabric application.

  .DESCRIPTION
      This script takes a root directory of a Service Fabric application and increments all package and manifest versions to allow for service fabric to be able to detect and deploy
      the new changes to the application. This is intended to be run in the source root directory of the Service Fabric application, to allow for the manifest version changes to be
      tracked in source control. The user will need to merge the changes to the manifest versions in their source control.

  .PARAMETER appFilePath
      MANDATORY [string] Service Fabric application root folder.

  .PARAMETER appTypeName
      MANDATORY [string] Service Fabric application type name of the desired application to have the manifests updated.

  .EXAMPLE
      .\IncrementManifestVersions.ps1 -appFilePath C:\onebranch\SRE\Main\src\Microsoft\Azure\ServiceFabric\test\SF-MAtemp\SF-MAtemp\MonitoringServiceWithMA -appTypeName FabricMonitoringServiceApplication

  .LINK
      https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-application-upgrade-tutorial
      https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-application-upgrade-tutorial-powershell

#>

param(
    [Parameter(Mandatory=$True)]
    [string]$appFilePath,
    [Parameter(Mandatory=$True)]
    [string]$appTypeName
)

function Increment-Version{
    Param(
        [Parameter(Mandatory=$True)]
        [string]$oldVersion
    )

    $old = [Version]$oldVersion
    $new = New-Object System.Version($old.Major,$old.Minor,$old.Build,($old.Revision+1))
    return $new.ToString()
}

function Increment-ServiceManifestVersion{
    param(
        [Parameter(Mandatory=$True)]
        [string]$serviceManifestPath,
        [Parameter(Mandatory=$False)]
        [string]$version
    )
    [xml]$serviceManifest = Get-Content $serviceManifestPath
    $serviceManifest.ServiceManifest.Version = Increment-Version -oldVersion $serviceManifest.ServiceManifest.Version
    $serviceManifest.Save($serviceManifestPath)
    Write-Debug "Service manifest $($serviceManifest.ServiceManifest.Name) version incremented to $($serviceManifest.ServiceManifest.Version)"
    return $serviceManifest.ServiceManifest.Version
}

function ApplicationManifestVersion-Increment{
    param(
        [Parameter(Mandatory=$True)]
        [string]$appPath,
        [Parameter(Mandatory=$False)]
        [string]$appTypeName
    )

    Write-Output "Incrementing..."
    $appManifestPaths = Get-ChildItem $appPath -Recurse -Filter "ApplicationManifest.xml"
    foreach($manifestPath in $appManifestPaths){
        [xml]$appManifest = Get-Content $manifestPath.FullName
        
        Write-Output "ManifestPath: $manifestPath"
        if($appManifest.ApplicationManifest.ApplicationTypeName -ne $appTypeName){
            Write-Output "Skipping $($appManifest.ApplicationManifest.ApplicationTypeName) location:$($manifestPath.FullName) due to not being correct application manifest to edit."
        }
        elseif($manifestPath.FullName -like '*\bin\*' -or $manifestPath.FullName -like '*\out\*'){
            Write-Output "Skipping $($appManifest.ApplicationManifest.ApplicationTypeName) location:$($manifestPath.FullName) due to path containing '*\bin\*' or '*\out\*'"
        }
        else{
            $svcManifests = Get-ChildItem $appPath -Recurse -Filter "ServiceManifest.xml"
            
            # update each package in service manifest
            $appManifest.ApplicationManifest.ServiceManifestImport | foreach {
                foreach($manifest in $svcManifests){
                    [xml]$svcManifest = Get-Content $manifest.FullName
                    if($svcManifest.ServiceManifest.Name -eq $_.ServiceManifestRef.ServiceManifestName){
                        PackageVersion-Increment -manifestFilePath $manifest.FullName
                        $_.ServiceManifestRef.ServiceManifestVersion = Increment-ServiceManifestVersion -serviceManifestPath $manifest.FullName
                    }
                }
            }
            $appManifest.ApplicationManifest.ApplicationTypeVersion = Increment-Version -oldVersion $appManifest.ApplicationManifest.ApplicationTypeVersion
            $appManifest.Save($manifestPath.FullName)
        
            Write-Debug "Application manifest version is now $($appManifest.ApplicationManifest.ApplicationTypeVersion) located here: $($manifestPath.FullName)"
            Write-Output "Completed"
        }        
    }
}

function PackageVersion-Increment{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$True)]
        [string]$manifestFilePath
    )

        [xml]$svcManifest = Get-Content $manifestFilePath

        # increment package version of package
        if($svcManifest.ServiceManifest.CodePackage){ 
            $svcManifest.ServiceManifest.CodePackage | foreach { 
                $_.Version = Increment-Version -oldVersion $_.Version
                Write-Debug "$($_.Name) version is $($_.Version)"
                }  
        }
        if($svcManifest.ServiceManifest.ConfigPackage){ 
            $svcManifest.ServiceManifest.ConfigPackage | foreach { 
                $_.Version = Increment-Version -oldVersion $_.Version
                Write-Debug "$($_.Name) version is $($_.Version)"
                }  
        }
        if($svcManifest.ServiceManifest.DataPackage){ 
            $svcManifest.ServiceManifest.DataPackage | foreach { 
                $_.Version = Increment-Version -oldVersion $_.Version
                Write-Debug "$($_.Name) version is $($_.Version)"
                } 
        }
        $svcManifest.Save($manifestFilePath)
}

ApplicationManifestVersion-Increment -appPath $appFilePath -appTypeName $appTypeName