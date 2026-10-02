# How To Deploy Monitoring Agent App in Service Fabric Cluster

This note has the basic steps of deploying monitoring agent application in Service Fabric cluster manually. The
deployment is needed when a SF cluster is not created by PubSub deployment but on Azure web portal, or when Vega
specific MA configuration is required.

## Location of MA App

After the build is completed, MA is stored at `out\Release-x64\MdsAgentApplication` directory.

## Application Parameters

DefaultApplicationParameters.xml contains default parameters for local dev cluster deployment. To deploy MA in a real SF
cluster, use the following parameters:

* AgentPkg_InstanceCount: change to -1 so MA process is running on all nodes;
* MAXStoreAccounts: this contains the Azure Storage account information encrypted using a certificate, and the
  certificate has to be installed on all nodes where MA is running.
* ClusterName: this is the "Tenant" name in Geneva. Choose a name unique and appropriate.
* DataCenterName: not in use, keep it as "localhost".
* MAConfigFileName: MA config file, keep it "VegaMAConfig.Test.xml".

The key is to get MAXStoreAccounts string, which can be retrieved from existing PubSub MA app. For instance, open
zTestCO1 in Service Fabric Explorer, go to MadariMsa in "Application View", click "fabric:/madarimsa" then
ApplicationParameters, copy the parameter there.

## Get the Certificate

The aforementioned certificate is stored in Secret Store, but we should know the location first. This is configured in
DCMT at `src\Configuration\SdnPubSub`. For test deployments, open `settings_SdnPubSub_Test.Test.xml`, search
`CLIENT_ENCRYPTION_CERTIFICATE`, replace the `%` enclosed environment variable with its real vlaue, then we have the
location, for instance

    AzureInfrastructure/Madari/MdsTestEncryption.pfx

Then the cert can be viewed by command:

    stashclient.exe -env:test cget -name:AzureInfrastructure/Madari/MdsTestEncryption.pfx

Use "cgetdecrypted" command (add "-File:*pfxFileName*) to export the cert to SAW device. Then this cert can be installed
either manually or automatically as a part of SF app startup.

## Install the Certificate

Cert must be installed at personal store at machine level, i.e. `cert:\LocalMachine\My` in PowerShell. The command to
install it:

    certutil -f -p [CertPassword] -ImportPfx My [PfxFileName] NoRoot

PFX password is in the output of "cgetdecrypted" command.

## Deployment Script

The script to deploy or undeploy SF app is in `out\Release-x64\CloudUnitTests`, to deploy MA:

    .\CloudUnitTests\Launch-ServiceFabricApplication.ps1 -ApplicationPackagePath .\MdsAgentApplication `
      -ApplicationParametersFile .\MdsAgentApplication\DefaultApplicationParameters.xml -Action Deploy `
      -ConnectionEndpoint [SF cluster DNS name]:19000 -ServerName [SF cluster DNS name] `
      -CertThumbprint [thumbprint] -StoreLocation CurrentUser

## Check the MA App

Firstly use web version of Service Fabric Explorer to see if MA is started. If it has any error, the most likely error
is the process fails to decrypt MAXStoreAccounts string. In this case, remote into the node where the problem is
happening, check MA log and look for the following pattern:
```
Error (2018-08-27T06:12:22Z): SecUtil - MAEventTable: Level:2 ActivityId: eb55441f-23a7-4d4b-bf7b-520a48c7667b MDRESULT:0x00000000 ErrorCode(-2146885620):Cannot find the certificate and private key to use for decryption. Message:There is no appropriate cert in the store LOCAL_MACHINE\MY to decrypt the data, the data must be decrypted with the cert with serial number 67e5549b43a817b240f2a2f83c4292e4 and issued by 'CN=monitoring.sdnpubsub.core.windows.net'. Please make sure this cert is installed to the store LOCAL_MACHINE\MY along with the corresponding private key.
Error (2018-08-27T06:12:22Z): SecUtil - MAEventTable: Level:2 ActivityId: eb55441f-23a7-4d4b-bf7b-520a48c7667b MDRESULT:0x80090003 ErrorCode(0): Message:Failed to decrypt data with certificate from store LOCAL_MACHINE\MY; will retry using store MY
```

Use stashclient to check if the MDS client cert has the same serial number as the one in the log, and confirm the cert
is installed properly.

Once MA is started, visit "Explorer" in "Agent" tab in [Jarvis website](https://jarvis-west.dc.ad.msft.net), check if
the SF cluster is in the list.

For any other questions, contact [zhyao](mailto:zhyao) for help.
