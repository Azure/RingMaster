<#
.SYNOPSIS
    Launches pubsub service in local Service Fabric simulation cluster

.PARAMETER Action
    Specifies the action to take, Start - deploy and start the app, Stop - stop and deregister, Install - install service
    fabric and setup local cluster, Uninstall - uninstall service fabric

.PARAMETER ServiceFabricPackage
    Specifies the list of application package to deploy and start.

.PARAMETER ClusterEndpoint
    Specifies the address of the real Service Fabric cluster to deploy pubsub.  Use local cluster by default.

.PARAMETER MetaDataEndpoint
    Specifies the address of the real Service Fabric cluster's MetaDataEndpoint. If Specified we use dSTS Auth. (Not used by default)

.PARAMETER ServerName
    Specifies the server name for connecting to real service fabric cluster.

.PARAMETER CertThumbprint
    Specifies the certificate for connecting to real service fabric cluster.

.PARAMETER SkipVersionCheck
    Specifies whether or not to check the service fabric version on the local host.

.PARAMETER ServiceFabricPowerShell
    Specifies the directory where the service fabric client package, or the PowerShell module, is located.

.PARAMETER WaitForReady
    Specifies whetehr or not to wait for application's Ready state after Deploy. Valid only with Deploy Action
#>

[CmdletBinding()]
param(
    [ValidateSet("Start", "Upgrade", "Deploy", "Stop", "Upsize", "Install", "Uninstall")]
    [string] $Action = "Deploy",

    [string] $ServiceFabricPackage = @("E:\RD\Networking\Vega\out\debug-AMD64\MdsAgentApplication-Pkg\MdsAgentApplication"),

    [string] $ApplicationParameterFile,

    [string] $ClusterEndpoint = "127.0.0.1:19000",
    
    [string] $MetaDataEndpoint = $null,

    [string] $ServerName = $null,

    [string] $CertThumbprint = $null,

    [string] $ServiceTypeName = "",

    [int] $ServiceInstanceCount = 3,

    [switch] $SkipVersionCheck = $false,

    [string] $ServiceFabricPowerShell = $null,

    [switch] $WaitForReady = $false
    )

Set-StrictMode -version latest
$ErrorActionPreference = "Stop"

$appType = $null
$serviceTypes = $null

$msiExe = Join-Path ${env:windir} "System32\msiexec.exe"
$fabricCodePath = ""
$fabricSDKInstallPath = ""

#######################################################################################################################

function WriteLine
{
    param(
        [Parameter(ValueFromPipeline = $true)]
        $Object,
        [ConsoleColor] $ForegroundColor = [ConsoleColor]"White"
        )

    Write-Host -ForegroundColor $ForegroundColor "$(Get-Date) : $Object"
}

function MakeAppName([string] $appTypeName)
{
    $a = $appTypeName
    if ($a.EndsWith("ServiceApplication")) {
        $a = $a.Replace("ServiceApplication", "")
    }

    return "fabric:/$a"
}

function MakeServiceName([string] $appTypeName, [string] $serviceTypeName, [int] $instance = 0)
{
    $a = MakeAppName $appTypeName
    $st = $serviceTypeName
    if ($st.EndsWith("ServiceType")) {
        $st = $st.Replace("ServiceType", "")
    }

    if ($st -eq "RingMasterService")
    {
        $incrInstance = $instance + 1;
        $s = "{0:D2}" -f $incrInstance;
        return "$a/$st/$s"
    }
    else {
        return "$a/$st"
    }
}

function ParseServiceFabricApplicationManifest
{
    param(
        [ValidateScript({Test-Path -PathType Container $_})]
        [string] $AppPackagePath
        )

    $manifest = [xml] (Get-Content (Join-Path $AppPackagePath "ApplicationManifest.xml"))
    $script:appType = New-Object PSObject -Property @{
        Name = $manifest.ApplicationManifest.ApplicationTypeName
        Version = $manifest.ApplicationManifest.ApplicationTypeVersion
    }

    $script:serviceTypes = @()

    $serviceManifestNames = $manifest.ApplicationManifest.ServiceManifestImport.ServiceManifestRef.ServiceManifestName
    foreach ($svcManifestName in $ServiceManifestNames) {
        $manifest = [xml] (Get-Content (Join-Path $AppPackagePath "$svcManifestName\ServiceManifest.xml"))

        try {
            $name = $manifest.ServiceManifest.ServiceTypes.StatefulServiceType.ServiceTypeName
        }
        catch {
            $name = $manifest.ServiceManifest.ServiceTypes.StatelessServiceType.ServiceTypeName
        }

        $script:serviceTypes += New-Object PSObject -Property @{
            Name = $name
            Version = $manifest.ServiceManifest.Version
        }
    }

    WriteLine "Application name and type are read from manifest"
    WriteLine "$($script:appType | Format-List -Force)"
}

function CheckServiceFabricVersion
{
    $regkey = 'HKLM:\SOFTWARE\Microsoft\Service Fabric'
    if (-not (Test-Path $regkey)) {
        WriteLine "Service Fabric is not installed"
        return $false
    }

    try {
        $ver  = (Get-ItemProperty -Path $regkey -Name FabricVersion).FabricVersion
        $root = (Get-ItemProperty -Path $regkey -Name FabricRoot).FabricRoot
        WriteLine "Service Fabric $ver installed at $root"

        $script:fabricCodePath = (Get-ItemProperty -Path $regkey -Name FabricCodePath).FabricCodePath

        $ver  = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Service Fabric SDK' -Name FabricSDKVersion).FabricSDKVersion
        $script:fabricSDKInstallPath = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Service Fabric SDK' -Name FabricSDKInstallPath).FabricSDKInstallPath
        WriteLine "Service Fabric SDK $ver installed at $fabricSDKInstallPath"

        return $true
    }
    catch {
        WriteLine "Service Fabric is not installed"
        return $false
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

function InstallServiceFabricLocalCluster
{
    WriteLine "Cleaning up local cluster..."

    $regkey = 'HKLM:\SOFTWARE\Microsoft\Service Fabric SDK'
    if (Test-Path $regkey) {
        $fabricSDKInstallPath = (Get-ItemProperty -Path $regkey -Name FabricSDKInstallPath).FabricSDKInstallPath
    }
    else {
        $fabricSDKInstallPath = Join-Path $env:ProgramFiles "Microsoft SDKs\Service Fabric"
    }

    ${env:PATH} = "${env:PATH};$fabricCodePath"
    cmd /c PowerShell -file "$fabricSDKInstallPath\ClusterSetup\CleanCluster.ps1"
    cmd /c rd /s/q "$($env:SystemDrive)\SfDevCluster"

    WriteLine "Installing and starting local cluster..."
    cmd /c PowerShell -file "$fabricSDKInstallPath\ClusterSetup\DevClusterSetup.ps1"
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
        if ([string]::IsNullOrEmpty($MetaDataEndpoint))
        {
            WriteLine "Connect-ServiceFabricCluster, default"
            WriteLine "ConnectionEndpoint:$ClusterEndpoint"
            Connect-ServiceFabricCluster -ConnectionEndpoint $ClusterEndpoint -Verbose
        }
        else
        {
            $connInitTimeout = 100
            $timeoutSec = 60

            WriteLine "Connect-ServiceFabricCluster through dSTS"
            WriteLine "ConnectionEndpoint:$ClusterEndpoint"
            WriteLine "MetaDataEndpoint:$MetaDataEndpoint"
            WriteLine "ServerCommonName:$ServerName"
            WriteLine "ConnectionInitializationTimeoutInSec:$connInitTimeout"
            WriteLine "TimeoutSec:$timeoutSec"
            Connect-ServiceFabricCluster -DSTS -ConnectionEndpoint $ClusterEndpoint -MetaDataEndpoint $MetaDataEndpoint -ServerCommonName $ServerName -Verbose `
                    -ConnectionInitializationTimeoutInSec $connInitTimeout -TimeoutSec $timeoutSec
        }
    }
    else {
        WriteLine "Connect-ServiceFabricCluster, Local certificate"
        WriteLine "ConnectionEndpoint:$ClusterEndpoint"
        WriteLine "Thumbprint:$CertThumbprint in LocaMachine\My"
        WriteLine "ServerCommonName:$ServerName"
        Connect-ServiceFabricCluster -ConnectionEndpoint $ClusterEndpoint -X509Credential -FindType FindByThumbprint `
            -FindValue $CertThumbprint -StoreLocation LocalMachine -StoreName My -ServerCommonName $ServerName -Verbose
    }

    # HACK: other service fabric cmdlets assume this variable is accessible
    $global:clusterConnection = $clusterConnection
}

function StartService
{
    param(
        [ValidateScript({Test-Path $_})]
        [string] $appPackagePath,
        [switch] $WaitForReady
        )

    ParseServiceFabricApplicationManifest $appPackagePath

    $param = @{}
    if (-not ([string]::IsNullOrEmpty($ApplicationParameterFile))) {
        $paramXml = [Xml] (Get-Content $ApplicationParameterFile)
        $paramXml.GetElementsByTagName("Parameter") | % {
            $param[$_.Name] = $_.Value
        }
    }

    $appName = MakeAppName $appType.Name

    # Must call ConnectServiceFabric first
    WriteLine "Get ImageStore connection string"
    $imageStoreConnectionString = GetImageStoreConnectionString
    WriteLine "ImageStore connection string: $imageStoreConnectionString"

    $appPathInImageStore = $appType.Name

    WriteLine "Try to get application by name [$appName]"
    if ((Get-ServiceFabricApplicationType $appType.Name | ? ApplicationTypeVersion -eq $appType.Version | measure | select -ExpandProperty Count) -gt 0)
    {
        WriteLine "Application $($appType.Name) $($appType.Version) is already registered"
    } else {
        WriteLine "Copy-ServiceFabricApplicationPackage"
        WriteLine "  AppPacakgePath:$appPackagePath"
        WriteLine "  imageStoreConnectionString:$imageStoreConnectionString"
        WriteLine "  appPathInImageStore:$appPathInImageStore"
        Copy-ServiceFabricApplicationPackage -Verbose `
            -ApplicationPackagePath $appPackagePath `
            -ImageStoreConnectionString $imageStoreConnectionString `
            -ApplicationPackagePathInImagestore $appPathInImageStore

        WriteLine "Register-ServiceFabricApplicationType -ApplicationPathInImageStore $appPathInImageStore"
        Register-ServiceFabricApplicationType -ApplicationPathInImageStore $appPathInImageStore -Verbose
    }

    # evaluate the application exists
    $appExists = ((Get-ServiceFabricApplication | ? ApplicationName -eq $appName | measure | select -ExpandProperty Count) -gt 0)
    if ($appExists) {
        WriteLine "Application [$appName] returned by Get-ServiceFabricApplication."
    } else {
        WriteLine "Application [$appName] didn't return by Get-ServiceFabricApplication."
    }

    if ($appExists) {
        WriteLine "Upgrading application instance [$appName]..."
        WriteLine "Parameters:"
        $param | Format-Table -AutoSize | Out-String -width 150 | WriteLine
        Start-ServiceFabricApplicationUpgrade -ApplicationName $appName `
            -ApplicationTypeVersion $appType.Version -ApplicationParameter $param `
            -Monitored -FailureAction Rollback -Verbose

        WriteLine "Upgrade has started, run this command to check the progress: Get-ServiceFabricApplicationUpgrade $appName" -ForegroundColor Cyan
    }
    else {
        WriteLine "Creating new application instance [$appName]..."
        WriteLine "Parameters:"
        $param | Format-Table -AutoSize | Out-String -width 150 | WriteLine
        New-ServiceFabricApplication -ApplicationName $appName `
            -ApplicationTypeName $appType.Name -ApplicationTypeVersion $appType.Version -ApplicationParameter $param -Verbose
            
        $serviceTypes | % {
            $serviceName = MakeServiceName $appType.Name $_.Name 0
            WriteLine "Creating new service instance [$serviceName]..."
            New-ServiceFabricServiceFromTemplate -ApplicationName $appName -ServiceName $serviceName -ServiceTypeName $_.Name -Verbose
        }
    }

    WriteLine "Verifying application and services..."
    $verify_appType = Get-ServiceFabricApplicationType -ApplicationTypeName $appType.Name
    if (-not $verify_appType) {
        throw "Verification failed: ApplicationType '$($appType.Name)' is not found"
    }
    $verify_serviceType = Get-ServiceFabricServiceType -ApplicationTypeName $appType.Name -ApplicationTypeVersion $appType.Version
    if (-not $verify_serviceType) {
        throw "Verification failed: ServiceType is not found, $($appType.Name) $($appType.Version)"
    }

    $application = Get-ServiceFabricApplication | ? ApplicationName -eq $appName
    if (-not $application) {
        throw "Verification failed: Application '$appName' is not found"
    }

    $failedService = @()
    $serviceTypes | % {
        $serviceName = MakeServiceName $appType.Name $_.Name 0
        $verify_service = Get-ServiceFabricService -ApplicationName $appName -ServiceName $serviceName
        if (-not $verify_service) {
            $failedService += $serviceName
        }
    }
    if ($failedService.Length -gt 0) {
        throw "Verifiction failed: one more more Service are not found, $failedService"
    }

    if ($WaitForReady) {
        $totalElapsedMinute = 0
        $sleepInSec = 60

        $appStatus = (Get-ServiceFabricApplication -ApplicationName $appName).ApplicationStatus

        while (($appStatus -ne 'Ready') -and ($totalElapsedMinute -lt 60)) {
            WriteLine "Sleeping $sleepInSec seconds"
            Start-Sleep -s $sleepInSec
            $totalElapsedMinute += 1

            $appStatus = (Get-ServiceFabricApplication -ApplicationName $appName).ApplicationStatus
        }

        $appStatus = (Get-ServiceFabricApplication -ApplicationName $appName).ApplicationStatus
        if ($appStatus -ne 'Ready') {
            throw "Application state '$appStatus' is not Ready yet."
        }
        
        $healthStatus = (Get-ServiceFabricApplication -ApplicationName $appName).HealthState
        if ($healthStatus -ne 'Ok') {
            throw "Health status '$healthStatus' is not Ok after ApplicationStatus becomes Ready."
        }
    }
}

function StopService
{
    param(
        [ValidateScript({Test-Path $_})]
        [string] $appPackagePath
        )

    ParseServiceFabricApplicationManifest $appPackagePath

    $appName = MakeAppName $appType.Name
    $serviceNames = Get-ServiceFabricService -ApplicationName $appName | % { $_.ServiceName.OriginalString }
    $ver = (Get-ServiceFabricApplication -ApplicationName $appName).ApplicationTypeVersion

    WriteLine "Deleting application and unregistering application type..."
    $serviceNames | % {
        WriteLine "Removing $_ ..."
        Remove-ServiceFabricService -ServiceName $_ -Force
    }
    
    Remove-ServiceFabricApplication -ApplicationName $appName -Force
    Unregister-ServiceFabricApplicationType -ApplicationTypeName $appType.Name -ApplicationTypeVersion $ver -Force
}

function UpsizeServices
{
    param(
        [ValidateScript({Test-Path $_})]
        [string] $appPackagePath
        )

    ParseServiceFabricApplicationManifest $appPackagePath

    # Get the existing number of instances
    $appName = MakeAppName $appType.Name
    $serviceNames = @()
    $serviceNames += Get-ServiceFabricService -ApplicationName $appName | ? {
        $_.ServiceTypeName -eq $ServiceTypeName
    } | % {
        $_.ServiceName.OriginalString
    }

    if ($serviceNames.Count -ge $ServiceInstanceCount) {
        WriteLine "$ServiceTypeName has $($serviceNames.Count) instances already"
        return
    }

    $lastName = $serviceNames | Sort -Unique | Select -Last 1

    # Find the index of the last one
    $n = [int]($lastName -split "/")[-1]
    ($n + 1)..$ServiceInstanceCount | % {
        $newName = "$appName/$($ServiceTypeName.ToLower())/{0:00}" -f $_

        WriteLine "Creating $newName from $ServiceTypeName ..."
        New-ServiceFabricServiceFromTemplate -ApplicationName $appName -ServiceName $newName -ServiceTypeName $ServiceTypeName
    }
}

#######################################################################################################################

# Checks if Service Fabric is installed on the local host, not on remote server
if ($ClusterEndpoint -eq "127.0.0.1:19000" -and (-not $SkipVersionCheck)) {
    $sfInstalled = CheckServiceFabricVersion
}
else {
    $sfInstalled = $true
}

# Deploy is new for deployment template. Start/Upgrade is legacy and CDP uses them.
if (($Action -eq "Deploy") -or ($Action -eq "Start") -or ($Action -eq "Upgrade")) {
    if (-not $sfInstalled) {
        throw "ServiceFabric not installed.  Invoke with 'Install' action to install it"
    }

    ConnectServiceFabric

    StartService $ServiceFabricPackage -WaitForReady:$WaitForReady
}
elseif ($Action -eq "Upsize") {
    if (-not $sfInstalled) {
        throw "ServiceFabric not installed.  Invoke with 'Install' action to install it"
    }

    ConnectServiceFabric

    UpsizeServices $ServiceFabricPackage
}
elseif ($Action -eq "Stop") {
    if (-not $sfInstalled) {
        throw "ServiceFabric not installed.  Invoke with 'Install' action to install it"
    }

    ConnectServiceFabric

    StopService $ServiceFabricPackage
}