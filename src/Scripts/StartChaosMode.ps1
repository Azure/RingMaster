[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [int] $TimeToRunMinute,

    [Parameter(Mandatory=$true)]
    [int] $MaxConcurrentFaults,

    [string] $ApplicationName = "fabric:/RingMaster",

    [int] $MaxClusterStabilizationTimeSecs = 30,

    [int] $WaitTimeBetweenIterationsSec = 10,

    [int] $WaitTimeBetweenFaultsSec = 0,

    [switch] $WaitForFinish = $false
)

# Passed-in cluster health policy is used to validate health of the cluster in between Chaos iterations. 
$clusterHealthPolicy = new-object -TypeName System.Fabric.Health.ClusterHealthPolicy
$clusterHealthPolicy.MaxPercentUnhealthyNodes = 100
$clusterHealthPolicy.MaxPercentUnhealthyApplications = 100
$clusterHealthPolicy.ConsiderWarningAsError = $False

# Describes a map, which is a collection of (string, string) type key-value pairs. The map can be used to record information about
# the Chaos run. There cannot be more than 100 such pairs and each string (key or value) can be at most 4095 characters long.
# This map is set by the starter of the Chaos run to optionally store the context about the specific run.
$context = @{"ReasonForStart" = "Testing"}

#List of cluster entities to target for Chaos faults.
$chaosTargetFilter = new-object -TypeName System.Fabric.Chaos.DataStructures.ChaosTargetFilter
$chaosTargetFilter.ApplicationInclusionList = new-object -TypeName "System.Collections.Generic.List[String]"
$chaosTargetFilter.ApplicationInclusionList.Add($ApplicationName)

$events = @{}
$now = [System.DateTime]::UtcNow

Start-ServiceFabricChaos -TimeToRunMinute $TimeToRunMinute -MaxConcurrentFaults $MaxConcurrentFaults `
    -MaxClusterStabilizationTimeoutSec $MaxClusterStabilizationTimeSecs -EnableMoveReplicaFaults `
    -WaitTimeBetweenIterationsSec $WaitTimeBetweenIterationsSec -WaitTimeBetweenFaultsSec $WaitTimeBetweenFaultsSec `
    -ClusterHealthPolicy $clusterHealthPolicy -ChaosTargetFilter $chaosTargetFilter

while($WaitForFinish)
{
    $stopped = $false
    $report = Get-ServiceFabricChaosReport -StartTimeUtc $now -EndTimeUtc ([System.DateTime]::MaxValue)

    foreach ($e in $report.History) {

        if(-Not ($events.Contains($e.TimeStampUtc.Ticks)))
        {
            $events.Add($e.TimeStampUtc.Ticks, $e)
            if($e -is [System.Fabric.Chaos.DataStructures.ValidationFailedEvent])
            {
                Write-Host -BackgroundColor White -ForegroundColor Red $e
            }
            else
            {
                Write-Host $e
                # When Chaos stops, a StoppedEvent is created.
                # If a StoppedEvent is found, exit the loop.
                if($e -is [System.Fabric.Chaos.DataStructures.StoppedEvent])
                {
                    return
                }
            }
        }
    }

    Start-Sleep -Seconds 1
}