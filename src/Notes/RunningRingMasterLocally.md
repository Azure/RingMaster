# Running RingMaster Locally

 ## Starting RingMaster server

 **Microsoft.RingMaster.RingMasterBackendTool.exe** is the command line tool that is used to start the *RingMaster* service and interact with it.

 For local testing, we will run RingMaster server without TLS.  

 Start RingMaster server with the following command.  99 is the port in which the server will listen for requets.

 ```
 Microsoft.RingMaster.RingMasterBackendTool.exe 99
 ```

 ## Starting RingMaster client

 The Powershell module **Microsoft.RingMaster.ClientModule.dll** can be used to interact with *RingMaster*.  Install the module with the following command:
 ````powershell
 Import-Module .\Microsoft.RingMaster.ClientModule.dll

 $s = Connect-RingMaster -ConnectionString "127.0.0.1:99"
 ````
 ## Interacting with RingMaster

 ### Get all child nodes under root
````powershell
Get-RingMasterNodeChildren -Session $s -Path /
````
### Create a new persistent node
````powershell
$data = [System.Guid]::NewGuid().ToByteArray()
Add-RingMasterNode -Session $s -Path /Node -Mode Persistent -Data $data
````

Creates a persistent node called *Node* under */* and associates the data *$data* with the node.

### Retrieve data from a node
````powershell
Get-RingMasterNodeData -Session $s -Path /Node
````
The data that was associated with the node in the previous command will be returned

### Create an ephemeral node
````powershell
Add-RingMasterNode -Session $s -Path /EphemeralNode -Mode Ephemeral
````
Creates an ephemeral node called *EphemeralNode* under */*.  Ephemeral nodes will automatically be deleted once the session that created the node is terminated.

### Delete a node
````powershell
Remove-RingMasterNode -Session $s -Path /Node -Version -1
````
Deletes the node */Node* regardless of what version of data it currently has.

### Create a node with no data
````powershell
Add-RingMasterNode -Session $s -Path /Parent -Mode Persistent
````

### Create a child node
````powershell
Add-RingMasterNode -Session $s -Path /Parent/Child -Mode Persistent
````

### Enumerate children of a node
````powershell
Get-RingMasterNodeChildren -Session $s -Path /Parent
````

### Change the data associated with a node
````powershell
$newdata = [System.Guid]::NewGuid().ToByteArray()
Set-RingMasterNodeData -Session $s -Path /Parent/Child -Version -1 -Data $newdata
````

Version number of **-1** indicates that data must be changed regardless of the current version of the data.

### Check if a node exists
````powershell
Get-RingMasterNodeStat -Session $s -Path /Parent
````
Prints **Stat** information for the node. This includes information like version of the data (*Version*), version of the ACL (*Aversion*) and version of the children collection(*Cversion*)

### Conditionally Change the data associated with a node
````powershell
Set-RingMasterData -Session $s -Path /Parent/Child -Version 1 -Data $newdata
````
Data will be changed only if the version matches the given version (*1*).  If the version doesn't match it, it means that the data was changed since the version was last checked.