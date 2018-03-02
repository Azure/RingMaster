# How to deploy RingMasterApplication

**RingMasterApplication:** `src\Applications\RingMaster`

## Deploying locally
You'll need a local Service Fabric cluster. Make sure you select the 5-node configuration.

### First things first
Switch to the parent directory of `RingMasterApplication`, and then:

1. Connect to the cluster.
2. Upload the package.
3. Register the application type.

````powershell
Connect-ServiceFabricCluster
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath .\RingMasterApplication -ApplicationPackagePathInImageStore RingMasterImage -TimeoutSec 300
Register-ServiceFabricApplicationType -ApplicationPathInImageStore RingMasterImage
````
### Configuring the application
If you look at the `ApplicationManifest.xml` in the root of the package, you'll see some settings:

````xml
    <Parameters>
      <!-- Note: These should be overridden in your Params Override file as needed -->
      <Parameter Name="EnvironmentName" DefaultValue="" />
      <Parameter Name="Tenant" DefaultValue="" />
      <Parameter Name="MdmAccountName" DefaultValue="" />
      <Parameter Name="MdmNamespace" DefaultValue="RingMasterWorker" />
      <Parameter Name="RingMasterWatchdog.MdmNamespace" DefaultValue="RingMasterWorker/Watchdog" />
      <Parameter Name="VIP" DefaultValue="" />
      <Parameter Name="RingMasterService_TargetReplicaSetSize" DefaultValue="1" />
      <Parameter Name="RingMasterService_MinReplicaSetSize" DefaultValue="1" />
      <Parameter Name="RingMasterWatchdog_InstanceCount" DefaultValue="1" />
    </Parameters>
````
You'll need to override `EnvironmentName`, `Tenant`, `MdmAccountName` and `VIP`, at a minimum. Fortunately, you specify these values as *parameters* when you create the instance:

````powershell
$params = @{
    EnvironmentName="Test";
    Tenant="StarTenant";
    MdmAccountName="SwanTest";
    VIP="10.80.30.25"
}

New-ServiceFabricApplication -ApplicationName fabric:/StarsRingMaster1 -ApplicationTypeName RingMasterApplication -ApplicationTypeVersion "1.0.0.96" -ApplicationParameter $params
````

Make sure you get the version correct! You can check it through the Service Fabric management portal.

### Testing the health

Use the management portal to verify that everything's green. Then, navigate to the primary replica of RingMasterService. You should see the RingMaster endpoint *(port 99)* and the ZooKeeper endpoint *(port 100.)*

Now, use **Microsoft.RingMaster.ClientModule** to connect:

````powershell
Import-Module .\Microsoft.Ringmaster.ClientModule.dll

$session = Connect-RingMaster -ConnectionString "10.80.30.25:99"
Get-RingMasterNodeChildren -Session $session -Path /
````

Success! Hooray!


