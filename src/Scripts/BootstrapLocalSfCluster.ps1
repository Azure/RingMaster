<#
.SYNOPSIS
    Bootstraps a local ServiceFabric cluster with 5 nodes, no certificate.  No SDK is required.
#>
[CmdletBinding()]
param(
    [string]
    $ClusterDataRoot = "c:\SfDevCluster\Data",

    [string]
    $ClusterLogRoot = "c:\SfDevCluster\Log"
    )

Set-StrictMode -version latest

$imageStore = Join-Path $ClusterDataRoot "ImageStoreShare"

# Copied from non-secure 5-node cluster manifest file in SDK.  Encoding is changed to utf-8, and all computer name
# is changed to localhost.
$clusterTemplate = '<?xml version="1.0" encoding="utf-8"?>
<ClusterManifest
    xmlns:xsd="http://www.w3.org/2001/XMLSchema"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns="http://schemas.microsoft.com/2011/01/fabric"
    Name="ComputerName-Local-Cluster"
    Version="1.0">
  <NodeTypes>
    <NodeType Name="NodeType0">
      <Endpoints>
        <ClientConnectionEndpoint Port="19000" />
        <LeaseDriverEndpoint Port="19001" />
        <ClusterConnectionEndpoint Port="19002" />
        <HttpGatewayEndpoint Port="19080" Protocol="http" />
        <ServiceConnectionEndpoint Port="19006" />
        <HttpApplicationGatewayEndpoint Port="19081" Protocol="http" />
        <ApplicationEndpoints StartPort="30001" EndPort="31000" />
      </Endpoints>
    </NodeType>
    <NodeType Name="NodeType1">
      <Endpoints>
        <ClientConnectionEndpoint Port="19010" />
        <LeaseDriverEndpoint Port="19011" />
        <ClusterConnectionEndpoint Port="19012" />
        <HttpGatewayEndpoint Port="19082" Protocol="http" />
        <ServiceConnectionEndpoint Port="19016" />
        <HttpApplicationGatewayEndpoint Port="19083" Protocol="http" />
        <ApplicationEndpoints StartPort="31001" EndPort="32000" />
      </Endpoints>
    </NodeType>
    <NodeType Name="NodeType2">
      <Endpoints>
        <ClientConnectionEndpoint Port="19020" />
        <LeaseDriverEndpoint Port="19021" />
        <ClusterConnectionEndpoint Port="19022" />
        <HttpGatewayEndpoint Port="19084" Protocol="http" />
        <ServiceConnectionEndpoint Port="19026" />
        <HttpApplicationGatewayEndpoint Port="19085" Protocol="http" />
        <ApplicationEndpoints StartPort="32001" EndPort="33000" />
      </Endpoints>
    </NodeType>
    <NodeType Name="NodeType3">
      <Endpoints>
        <ClientConnectionEndpoint Port="19030" />
        <LeaseDriverEndpoint Port="19031" />
        <ClusterConnectionEndpoint Port="19032" />
        <HttpGatewayEndpoint Port="19086" Protocol="http" />
        <ServiceConnectionEndpoint Port="19036" />
        <HttpApplicationGatewayEndpoint Port="19087" Protocol="http" />
        <ApplicationEndpoints StartPort="33001" EndPort="34000" />
      </Endpoints>
    </NodeType>
    <NodeType Name="NodeType4">
      <Endpoints>
        <ClientConnectionEndpoint Port="19040" />
        <LeaseDriverEndpoint Port="19041" />
        <ClusterConnectionEndpoint Port="19042" />
        <HttpGatewayEndpoint Port="19088" Protocol="http" />
        <ServiceConnectionEndpoint Port="19046" />
        <HttpApplicationGatewayEndpoint Port="19089" Protocol="http" />
        <ApplicationEndpoints StartPort="34001" EndPort="35000" />
      </Endpoints>
    </NodeType>
  </NodeTypes>
  <Infrastructure>
    <WindowsServer IsScaleMin="true">
      <NodeList>
        <Node NodeName="_Node_0" IPAddressOrFQDN="localhost" IsSeedNode="true"  NodeTypeRef="NodeType0" FaultDomain="fd:/0" UpgradeDomain="0" />
        <Node NodeName="_Node_1" IPAddressOrFQDN="localhost" IsSeedNode="true"  NodeTypeRef="NodeType1" FaultDomain="fd:/1" UpgradeDomain="1" />
        <Node NodeName="_Node_2" IPAddressOrFQDN="localhost" IsSeedNode="true"  NodeTypeRef="NodeType2" FaultDomain="fd:/2" UpgradeDomain="2" />
        <Node NodeName="_Node_3" IPAddressOrFQDN="localhost" IsSeedNode="false" NodeTypeRef="NodeType3" FaultDomain="fd:/3" UpgradeDomain="3" />
        <Node NodeName="_Node_4" IPAddressOrFQDN="localhost" IsSeedNode="false" NodeTypeRef="NodeType4" FaultDomain="fd:/4" UpgradeDomain="4" />
      </NodeList>
    </WindowsServer>
  </Infrastructure>
  <FabricSettings>
    <Section Name="Security">
      <Parameter Name="ClusterCredentialType" Value="None" />
      <Parameter Name="ServerAuthCredentialType" Value="None" />
    </Section>
    <Section Name="FailoverManager">
      <!-- expected cluster size allows the placement to start when the cluster is started. This value should be less than total number of nodes
                 as without it the FailoverManager will not start the placement of the user services. This value should be 80% to 90% of the cluster size.
            -->
      <Parameter Name="ExpectedClusterSize" Value="4" />
      <!-- The default target and min replica set sizes are 7 and 5. The below configuration is not required for cluster that have 7 or more nodes.  -->
      <Parameter Name="TargetReplicaSetSize" Value="5" />
      <Parameter Name="MinReplicaSetSize" Value="3" />
      <Parameter Name="ReconfigurationTimeLimit" Value="20" />
      <Parameter Name="BuildReplicaTimeLimit" Value="20" />
      <Parameter Name="CreateInstanceTimeLimit" Value="20" />
      <Parameter Name="PlacementTimeLimit" Value="20" />
    </Section>
    <Section Name="ReconfigurationAgent">
      <Parameter Name="ServiceApiHealthDuration" Value="20" />
      <Parameter Name="ServiceReconfigurationApiHealthDuration" Value="20" />
      <Parameter Name="LocalHealthReportingTimerInterval" Value="5" />
      <Parameter Name="IsDeactivationInfoEnabled" Value="true" />
      <Parameter Name="RAUpgradeProgressCheckInterval" Value="3" />
    </Section>
    <Section Name="ClusterManager">
      <!-- The default target and min replica set sizes are 7 and 5. The below configuration is not required for cluster that have 7 or more nodes.  -->
      <Parameter Name="TargetReplicaSetSize" Value="5" />
      <Parameter Name="MinReplicaSetSize" Value="3" />
      <Parameter Name="UpgradeStatusPollInterval" Value="5" />
      <Parameter Name="UpgradeHealthCheckInterval" Value="5" />
      <Parameter Name="FabricUpgradeHealthCheckInterval" Value="5" />
    </Section>
    <Section Name="NamingService">
      <!-- The default target and min replica set sizes are 7 and 5. The below configuration is not required for cluster that have 7 or more nodes.  -->
      <Parameter Name="TargetReplicaSetSize" Value="5" />
      <Parameter Name="MinReplicaSetSize" Value="3" />
    </Section>
    <Section Name="Management">
      <Parameter Name="ImageStoreConnectionString" Value="file:$imageStore" />
      <Parameter Name="ImageCachingEnabled" Value="false" />
      <Parameter Name="EnableDeploymentAtDataRoot" Value="true"/>
    </Section>
    <Section Name="Hosting">
      <Parameter Name="EndpointProviderEnabled" Value="true" />
      <Parameter Name="RunAsPolicyEnabled" Value="true" />
      <Parameter Name="DeactivationScanInterval" Value="60" />
      <Parameter Name="DeactivationGraceInterval" Value="10" />
      <Parameter Name="EnableProcessDebugging" Value="true" />
      <Parameter Name="ServiceTypeRegistrationTimeout" Value="20" />
      <Parameter Name="CacheCleanupScanInterval" Value="300" />
    </Section>
    <Section Name="HttpGateway">
      <Parameter Name="IsEnabled" Value="true" />
    </Section>
    <Section Name="PlacementAndLoadBalancing">
      <!-- balance the load on the cluster every 5 minutes.  -->
      <Parameter Name="MinLoadBalancingInterval" Value="300" />
    </Section>
    <Section Name="Federation">
      <Parameter Name="NodeIdGeneratorVersion" Value="V4" />
      <Parameter Name="UnresponsiveDuration" Value="0" />
    </Section>
    <Section Name="ApplicationGateway/Http">
      <Parameter Name="IsEnabled" Value="true" />
    </Section>
    <Section Name="FaultAnalysisService">
      <Parameter Name="TargetReplicaSetSize" Value="5" />
      <Parameter Name="MinReplicaSetSize" Value="3" />
    </Section>
    <Section Name="Trace/Etw">
      <Parameter Name="Level" Value="4" />
    </Section>
    <!-- Configure the DCA to cleanup the log folder only. The collection of the logs, performance counters and crashdumps is not performed on the local machine. -->
    <Section Name="Diagnostics">
      <Parameter Name="ProducerInstances" Value="ServiceFabricEtlFile, ServiceFabricPerfCtrFolder" />
      <Parameter Name="MaxDiskQuotaInMB" Value="10240" />
    </Section>
    <Section Name="ServiceFabricEtlFile">
      <Parameter Name="ProducerType" Value="EtlFileProducer" />
      <Parameter Name="IsEnabled" Value="true" />
      <Parameter Name="EtlReadIntervalInMinutes" Value=" 5" />
      <Parameter Name="DataDeletionAgeInDays" Value="3" />
    </Section>
    <Section Name="ServiceFabricPerfCtrFolder">
      <Parameter Name="ProducerType" Value="FolderProducer" />
      <Parameter Name="IsEnabled" Value="true" />
      <Parameter Name="FolderType" Value="ServiceFabricPerformanceCounters" />
      <Parameter Name="DataDeletionAgeInDays" Value="3" />
    </Section>
    <Section Name="TransactionalReplicator">
      <Parameter Name="CheckpointThresholdInMB" Value="64" />
    </Section>
  </FabricSettings>
</ClusterManifest>' -replace '\$imageStore', $imageStore

$templateFilePath = Join-Path $env:LOCALAPPDATA "devcluster.xml"
$clusterTemplate | Out-File -Encoding utf8 -Force -FilePath $templateFilePath

Write-Host "Testing cluster manifest file..."
Test-ServiceFabricClusterManifest -ClusterManifestPath $templateFilePath

New-Item -type directory -force $ClusterDataRoot
New-Item -type directory -force $ClusterLogRoot
New-Item -type directory -force $imageStore

Write-Host "Stopping fabric host service..."
Stop-Service FabricHostSvc -WarningAction SilentlyContinue

New-ServiceFabricNodeConfiguration -ClusterManifest $templateFilePath -FabricDataRoot $ClusterDataRoot -FabricLogRoot $ClusterLogRoot -RunFabricHostServiceAsManual

$sdkRegPath = 'HKLM:\Software\Microsoft\Service Fabric SDK'
if (Test-Path $sdkRegPath) {
    Set-ItemProperty $sdkRegPath -Name LocalClusterNodeCount -Value 5
}

Write-Host "Starting fabric host service..."
Start-Service FabricHostSvc -WarningAction SilentlyContinue

[HashTable]$connParams = @{}
$connParams.Add("ConnectionEndpoint", "localhost:19000")
$connParams.Add("TimeoutSec", 10)
$connParams.Add("WarningAction", 'SilentlyContinue')

# Waiting for the cluster to be started
while ($true) {
    try {
        Write-Host -ForegroundColor Cyan "$([DateTime]::Now) Connecting to local cluster"
        Connect-ServiceFabricCluster @connParams

        $testWarnings = @()
        $isConnSuccesfull = Test-ServiceFabricClusterConnection -TimeoutSec 5 -WarningAction SilentlyContinue -WarningVariable testWarnings
        if ($isConnSuccesfull -and ($testWarnings.Count -eq 0)) {
            Write-Host "Local Cluster ready status: 100% completed."
            break
        }
    }
    catch [System.Exception] {}
}

# Waiting for the naming service to be up
while ($true) {
    Write-Host -ForegroundColor Cyan "$([DateTime]::Now) Getting NamingService partitions..."
    $nsPartitions = Get-ServiceFabricPartition -ServiceName "fabric:/System/NamingService"
    $isReady = $true

    foreach ($partition in $nsPartitions) {
        if(!$partition.PartitionStatus.Equals([System.Fabric.Query.ServicePartitionStatus]::Ready)) {
            $isReady = $false
            break
        }
    }

    if ($isReady) {
        Write-Host "Naming Service is ready now..."
        break
    }

    Start-Sleep -Seconds 5
}
