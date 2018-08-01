<#
.SYNOPSIS
    Launches specified application to a Service Fabric cluster.

.PARAMETER ApplicationPackagePath
    Specifies the path to the application package.

.PARAMETER ApplicationParametersFile
    Xml of the parameters to be used for the application.

.PARAMETER Action
    Specifies the action to take.
    Deploy - deploy the application (default). Will automatically start services and upgrade as needed.
    Remove - Stop and deregister the application and services.

.PARAMETER ConnectionEndpoint
    Specifies the address to the Service Fabric cluster. Use local cluster by default.

.PARAMETER ServerName
    Specifies the server name for connecting to the service fabric cluster. Only needed if using certificates.
    By default, the ServerName is gotten from ConnectionEndPoint.

.PARAMETER CertThumbprint
    Specifies the certificate for connecting to the service fabric cluster. Specify the ServerName aswell.

.PARAMETER Timeout
    Specifies the Timeout for connecting to the service fabric cluster.

.PARAMETER StoreLocation
    Specifies the location of the store to retrive the certificate from. Only needed if using certificates.

.PARAMETER ServiceFabricPowerShell
    Specifies the directory where the service fabric client package, or the PowerShell module, is located.

.PARAMETER WaitForReady
    Specifies whether or not to wait for application's Ready state after Deployment. Valid only with Deploy Action.
    Default value is $false.
#>

[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [ValidateScript({Test-Path -PathType Container $_})]
    [String] $ApplicationPackagePath,

    [Parameter(Mandatory=$true)]
    [ValidateScript({Test-Path -PathType Leaf $_})]
    [string] $ApplicationParametersFile,

    [ValidateSet("Deploy", "Remove")]
    [string] $Action = "Deploy",

    [ValidateScript({$_.Contains(":")})]
    [string] $ConnectionEndpoint = "127.0.0.1:19000",

    [string] $ServerName = $null,

    [string] $CertThumbprint = $null,

    [int] $Timeout = 5,

    [ValidateSet("CurrentUser", "LocalMachine")]
    [string] $StoreLocation = "CurrentUser",

    [string] $ServiceFabricPowerShell = $null,

    [switch] $WaitForReady = $false
)

Set-StrictMode -version latest
$ErrorActionPreference = "Stop"

$appType = $null
$serviceTypes = $null

# Converts parameters within xml file into a hashtable
$applicationParameters = @{}
$applicationParametersXml = [Xml] (Get-Content $ApplicationParametersFile)
$applicationName = $applicationParametersXml.Application.Name
$applicationParametersXml.GetElementsByTagName("Parameter") | % {
    $applicationParameters[$_.Name] = $_.Value
}

# Get server name from connection end point if not given
if (-not ([string]::IsNullOrEmpty($CertThumbprint))) {
    if ([string]::IsNullOrEmpty($ServerName)) {
        $ServerName = $ConnectionEndpoint.Split(":")[0]
    }
}

#######################################################################################################################

function WriteLine
{
    Param(
        [Parameter(ValueFromPipeline = $true)]
        $Object,

        [ConsoleColor] $ForegroundColor = [ConsoleColor]"White"
    )

    Write-Host -ForegroundColor $ForegroundColor "$(Get-Date) : $Object"
}

function ParseServiceFabricApplicationManifest
{
    Param(
        [ValidateScript({Test-Path -PathType Container $_})]
        [string] $ApplicationPackagePath
    )

    $manifest = [xml] (Get-Content (Join-Path $ApplicationPackagePath "ApplicationManifest.xml"))
    $script:appType = New-Object PSObject -Property @{
        Name = $manifest.ApplicationManifest.ApplicationTypeName
        Version = $manifest.ApplicationManifest.ApplicationTypeVersion
    }

    $script:serviceTypes = @()

    $serviceManifestNames = $manifest.ApplicationManifest.ServiceManifestImport.ServiceManifestRef.ServiceManifestName
    foreach ($serviceManifestName in $ServiceManifestNames) {
        $manifest = [xml] (Get-Content (Join-Path (Join-Path $ApplicationPackagePath $serviceManifestName) "ServiceManifest.xml"))

        $isStateful = $false
        if ($manifest.ServiceManifest.ServiceTypes["StatefulServiceType"]) {
            $isStateful = $true
        }

        try {
            $name = $manifest.ServiceManifest.ServiceTypes.StatefulServiceType.ServiceTypeName
        }
        catch {
            $name = $manifest.ServiceManifest.ServiceTypes.StatelessServiceType.ServiceTypeName
        }

        $script:serviceTypes += New-Object -TypeName PSObject -Prop @{
            Name = $name
            Version = $manifest.ServiceManifest.Version
            IsStateful = $isStateful
        }
    }
}

function IsLocalClusterSetup
{
    try
    {
        if ((Get-ServiceFabricNodeConfiguration -ErrorAction SilentlyContinue) -eq $null)
        {
            return $false;
        }

        return $true;
    }
    catch [System.Exception]
    {
        return $false;
    }
}

function IsLocalClusterRunning
{
    try
    {
        $fabricService = Get-Service -Name "FabricHostSvc" -ErrorAction Ignore

        if ($fabricService -eq $null -or $fabricService.Status -ne "Running")
        {
            return $false;
        }

        return $true;
    }
    catch [System.Exception]
    {
        return $false;
    }
}

function GetImageStoreConnectionString
{
    [xml] $clusterManifest = Get-ServiceFabricClusterManifest
    $managementSection = $clusterManifest.ClusterManifest.FabricSettings.Section | ? { $_.Name -eq "Management" }
    $connectionString = $managementSection.Parameter | ? { $_.Name -eq "ImageStoreConnectionString" } | Select-Object -Expand Value

    return $connectionString
}

function ConnectServiceFabric
{
    if (-not ([string]::IsNullOrEmpty($ServiceFabricPowerShell))) {
        Import-Module $ServiceFabricPowerShell
    }

    if (-not $?) {
        throw "Failed to load ServiceFabric PowerShell module"
    }

    if ([string]::IsNullOrEmpty($CertThumbprint))
    {
        WriteLine "Connecting to $ConnectionEndpoint cluster"
        Connect-ServiceFabricCluster -ConnectionEndpoint $ConnectionEndpoint -Timeout $Timeout -Verbose
    }
    else {
        WriteLine "Connecting to $ConnectionEndpoint cluster using local certificate $CertThumbprint in $StoreLocation\My"
        Connect-ServiceFabricCluster -ConnectionEndpoint $ConnectionEndpoint -X509Credential `
        -ServerCommonName $ServerName -ServerCertThumbprint $CertThumbprint -FindType FindByThumbprint `
        -StoreLocation $StoreLocation -StoreName My -FindValue $CertThumbprint -Timeout $Timeout -Verbose
    }

    # REQUIRED: other service fabric cmdlets assume this variable is accessible
    $global:clusterConnection = $clusterConnection
}

function DeployService
{
    Param(
        [ValidateScript({Test-Path $_})]
        [string] $ApplicationPackagePath,
        [switch] $Wait
    )

    ParseServiceFabricApplicationManifest $ApplicationPackagePath

    # Note: ConnectServiceFabric needs to be called first
    $imageStoreConnectionString = GetImageStoreConnectionString

    $appPathInImageStore = $appType.Name

    if ((Get-ServiceFabricApplicationType $appType.Name | ? ApplicationTypeVersion -eq $appType.Version | measure | select -ExpandProperty Count) -gt 0) {
        WriteLine "Application $($appType.Name) $($appType.Version) is already registered. Using existing version."
    } else {
        WriteLine "Copying $ApplicationPackagePath to $imageStoreConnectionString -> $appPathInImageStore"
        Copy-ServiceFabricApplicationPackage -Verbose `
            -ApplicationPackagePath $ApplicationPackagePath `
            -ImageStoreConnectionString $imageStoreConnectionString `
            -ApplicationPackagePathInImagestore $appPathInImageStore

        WriteLine "Registering service fabric application $appPathInImageStore"
        Register-ServiceFabricApplicationType -ApplicationPathInImageStore $appPathInImageStore -Verbose
    }

    if ((Get-ServiceFabricApplication | ? ApplicationName -eq $applicationName | measure | select -ExpandProperty Count) -gt 0) {
        if ((Get-ServiceFabricApplication | ? ApplicationName -eq $applicationName | ? ApplicationTypeVersion -eq $appType.Version | measure | select -ExpandProperty Count) -gt 0) {
            throw "Application $applicationName $($appType.Version) already exists. Use Remove to delete the application before Deployment."
        }

        WriteLine "Upgrading application instance $applicationName with the following parameters..."
        $applicationParameters | Format-Table -AutoSize | Out-String -width 150 | WriteLine

        Start-ServiceFabricApplicationUpgrade -ApplicationName $applicationName `
            -ApplicationTypeVersion $appType.Version -ApplicationParameter $applicationParameters `
            -Monitored -FailureAction Rollback -Verbose

        WriteLine "Upgrade has started, run this command to check the progress: Get-ServiceFabricApplicationUpgrade $applicationName" -ForegroundColor Cyan
    } else {
        WriteLine "Creating new application instance $applicationName with the following parameters..."
        $applicationParameters | Format-Table -AutoSize | Out-String -width 150 | WriteLine

        New-ServiceFabricApplication -ApplicationName $applicationName `
            -ApplicationTypeName $appType.Name -ApplicationTypeVersion $appType.Version -ApplicationParameter $applicationParameters -Verbose

        foreach($service in $serviceTypes) {
            $serviceName = "$applicationName/$($service.Name)"
            if (-not (Get-ServiceFabricService -ApplicationName $applicationName -ServiceName $serviceName)) {
                WriteLine "Creating new service instance $serviceName..."
                try {
                    New-ServiceFabricServiceFromTemplate -ApplicationName $applicationName -ServiceName $serviceName -ServiceTypeName $service.Name -Verbose
                } catch [System.Fabric.FabricElementNotFoundException] {
                    if ($_.Exception.Message -NotLike "Service Type template not found") {
                        throw
                    }

                    WriteLine "No template found. Creating service $serviceName as Singleton partition with 1 instance count."
                    if ($service.IsStateful) {
                        New-ServiceFabricService -Stateful -PartitionSchemeSingleton -ApplicationName $applicationName -ServiceName $serviceName -ServiceTypeName $service.Name -InstanceCount 1 -Verbose
                    } else {
                        New-ServiceFabricService -Stateless -PartitionSchemeSingleton -ApplicationName $applicationName -ServiceName $serviceName -ServiceTypeName $service.Name -InstanceCount 1 -Verbose
                    }
                }
            } else {
                WriteLine "Service instance $serviceName already exists..."
            }
        }
    }

    WriteLine "Verifying application and services..."
    if (-not (Get-ServiceFabricApplicationType -ApplicationTypeName $appType.Name)) {
        throw "Verification failed: ApplicationType $($appType.Name) is not found."
    }

    if (-not (Get-ServiceFabricServiceType -ApplicationTypeName $appType.Name -ApplicationTypeVersion $appType.Version)) {
        throw "Verification failed: ServiceType $($appType.Name) $($appType.Version) is not found."
    }

    if (-not (Get-ServiceFabricApplication | ? ApplicationName -eq $applicationName)) {
        throw "Verification failed: Application $applicationName is not found."
    }

    $failedService = @()
    $serviceTypes | % {
        $serviceName = "$applicationName/$($_.Name)"
        if (-not (Get-ServiceFabricService -ApplicationName $applicationName -ServiceName $serviceName)) {
            $failedService += $serviceName
        }
    }

    if ($failedService.Length -gt 0) {
        throw "Verifiction failed: the following Services are not found, $failedService"
    }

    if ($Wait) {
        $totalElapsedMinute = 0
        $sleepInSec = 60

        $appStatus = (Get-ServiceFabricApplication -ApplicationName $applicationName).ApplicationStatus

        while (($appStatus -ne 'Ready') -and ($totalElapsedMinute -lt 60)) {
            WriteLine "Sleeping $sleepInSec seconds"
            Start-Sleep -s $sleepInSec
            $totalElapsedMinute += 1

            $appStatus = (Get-ServiceFabricApplication -ApplicationName $applicationName).ApplicationStatus
        }

        $appStatus = (Get-ServiceFabricApplication -ApplicationName $applicationName).ApplicationStatus
        if ($appStatus -ne 'Ready') {
            throw "Application state '$appStatus' is not Ready yet."
        }

        $healthStatus = (Get-ServiceFabricApplication -ApplicationName $applicationName).HealthState
        if ($healthStatus -ne 'Ok') {
            throw "Health status '$healthStatus' is not Ok after ApplicationStatus becomes Ready."
        }
    }
}

function RemoveService
{
    Param(
        [ValidateScript({Test-Path $_})]
        [string] $ApplicationPackagePath
    )

    ParseServiceFabricApplicationManifest $ApplicationPackagePath
    WriteLine "Deleting Application $applicationName and unregistering application type $($appType.Name)..."

    try {
        $serviceNames = Get-ServiceFabricService -ApplicationName $applicationName | % { $_.ServiceName.OriginalString }

        $serviceNames | % {
            WriteLine "Removing Service $_ ..."
            Remove-ServiceFabricService -ServiceName $_ -Force -Verbose
        }
    } catch {
        WriteLine "No services for the Application $applicationName found."
    }

    try {
        Remove-ServiceFabricApplication -ApplicationName $applicationName -Force -Verbose
    } catch {
        WriteLine "No Application $applicationName found."
    }

    try {
        $Local:version = (Get-ServiceFabricApplication -ApplicationName $applicationName).ApplicationTypeVersion
    } catch {
        $Local:version = $appType.Version
    }

    try {
        Unregister-ServiceFabricApplicationType -ApplicationTypeName $appType.Name -ApplicationTypeVersion $version -Force -Verbose
    } catch {
        WriteLine "No registered application $($appType.Name) $version found."
    }
}

#######################################################################################################################

# Checks if a local cluster is setup and running
if (($ConnectionEndpoint -eq "127.0.0.1:19000") -And
   ((-not (IsLocalClusterSetup)) -Or (-not (IsLocalClusterRunning)))) {
    throw "Local Cluster is not running. Setup a local cluster first."
}

ConnectServiceFabric
if ($Action -eq "Deploy") {
    DeployService $ApplicationPackagePath -WaitForReady $WaitForReady
} else {
    RemoveService $ApplicationPackagePath
}