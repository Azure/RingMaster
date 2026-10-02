[CmdletBinding()]
param(
    [string] $TestName = $null,

    [ValidateSet("Deploy", "Undeploy", "Setup", "SetupAndDeploy", "UndeployAndCleanup", "Cleanup")]
    [string] $Action,

    [int] $TimeoutInMinute = 10
    )

Set-StrictMode -version latest

$exitCode = 0

function Deploy
{
    & .\CloudUnitTests\Deploy-ServiceFabricApplication.ps1 -ApplicationPackagePath RingMasterApplication-Pkg\RingMasterApplication `
        -ApplicationParametersFile RingMasterApplication-Pkg\Local.5Node.xml
}

function Setup
{
    & .\CloudUnitTests\BootstrapLocalSfCluster.ps1
}

function Cleanup
{
    Write-Host -ForegroundColor Cyan "Uninstalling ServiceFabric..."
    ServiceFabric.XCopyPackage\UninstallFabric.ps1
}

function Undeploy
{
    & .\CloudUnitTests\Remove-ServiceFabricApplication.ps1 -ApplicationPackagePath RingMasterApplication-Pkg\RingMasterApplication `
        -ApplicationParametersFile RingMasterApplication-Pkg\Local.5Node.xml -Force
}

function Wait
{
    $ready = $FALSE
    Connect-ServiceFabricCluster -ConnectionEndpoint localhost:19000
    $startTime = [DateTime]::UtcNow

    while (-not $ready)
    {
        Start-Sleep 30
        $ringMaster = Get-ServiceFabricApplicationHealth -ApplicationName fabric:/RingMaster

        $ringMasterHealth = $ringMaster.AggregatedHealthState
        Write-Host -foreground Yellow "RingMaster health: $ringMasterHealth"

        if ($ringMasterHealth -ne "Ok"){
            Write-Host $ringMaster.UnhealthyEvaluations
        }

        $ready = $ringMasterHealth -eq "Ok"

        if (([DateTime]::UtcNow - $startTime).TotalMinutes -gt $TimeoutInMinute) {
            $script:exitCode = -1
            throw "Services do not become ready after waiting for $TimeoutInMinute minutes."
        }
    }
}

$env:PATH += ";${env:ProgramFiles}\Microsoft Service Fabric\bin\Fabric\Fabric.Code"

switch ($Action) {
    "Deploy" {
        Deploy
    }
    "Undeploy" {
        Undeploy
    }
    "Setup" {
        Setup
    }
	"SetupAndDeploy" {
		Setup
		Deploy
		Wait
	}
	"UndeployAndCleanup" {
		Undeploy
		Cleanup
	}
    "Cleanup" {
        Cleanup
    }
}

exit $exitCode
