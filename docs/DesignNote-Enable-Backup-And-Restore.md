# Service Fabric Stateful Service Backup and Restore 

## Introduction 

Service fabric is a high available and reliable distributed system platform. For a service hosted on service fabric 
there're typically 5 replicas so it can tolerant 2 failures. However in some scenarios data loss or corruption can still 
happen, for example, the administrator erroneously deletes the service, or bugs in the software caused data corruption. 
Therefore it is useful to periodically backup the data in a stateful service, and restore it in case of above data loss 
or corruption scenarios. This documentation introduces how to enable periodical backup in Vega, by mainly referring to 
[this service fabric documentation][1], and also talks about the issues I encountered and how to resolve them. 

## Prerequisites 

Please refer to the [aforementioned documentation][1] for all prerequisites. 

## Enable backup and restore service 

The aforementioned documentation talks about the new fields in resource templates that need to be added to enable the 
backup and restore service in service fabric, but it does not mention how to update the resource template for an 
existing cluster. To update the template we can refer to [this documentation][2]. After the template file is updated, 
the service fabric cluster will automatically begin to upgrade the "System" application. After upgrade complete there 
will be one more service in System: fabric:/System/BackupRestoreService. 

## Enable periodic backup for reliable stateful service 

Please refer to the [aforementioned documentation][1] for how to setup periodic backup for a reliable stateful service. 
Basically a set of REST APIs are provided for creating backup policies (the storage account information, backup 
schedule, retention policy, etc.), enabling backup on different hierarchy (applications, services or partitions), and 
restoring backups. The documentation also shows how to use Powershell to call the REST API with a certificate. At 
first I encountered an error invoking the REST API: "Invoke-WebRequest : The underlying connection was closed: Could not 
establish trust relationship for the SSL/TLS secure channel.". Later I found a work around in [this post][3]. Then I can 
invoke the APIs successfully. 

## How to restore backups 

Refer to [this documentation][4] on how to restore data from backups. Basically there're two approaches. One is 
automatic data restore in case of data loss. This can be configured when creating backup policies. To test this, after 
backups have been created, the Powershell command "Start-ServiceFabricPartitionDataLoss" can be used to trigger a data 
loss fault. The service should automatically recover and restore from the latest backup. If not using automatic restore, 
we can use the REST API to manually trigger a restore with a specific backup id. We can get the backup id by listing all 
backups or backups within a certain time range with the GetBackups API. 

[1]: https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-backuprestoreservice-quickstart-azurecluster 
[2]: https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-cluster-config-upgrade-azure 
[3]: https://stackoverflow.com/questions/11696944/powershell-v3-invoke-webrequest-https-error 
[4]: https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-backup-restore-service-trigger-restore 