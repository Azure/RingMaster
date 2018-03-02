# RingMaster Client Powershell Module

The *RingMasterClientModule* can be used to interact with RingMaster.

## Connect to RingMaster
```` powershell
$session = Connect-RingMaster -ConnectionString "127.0.0.1:99"
````
## Create a node
```` powershell
Add-RingMasterNode -Session $session -Path "/NewNode"
````
## Get the names of the children of a node
```` powershell
Get-RingMasterNodeChildren -Session $session -Path "/"
````
## Get the data associated with a node
```` powershell
Get-RingMasterNodeData -Session $session -Path "/Foo"
````
## Get the ACL associated with a node
```` powershell
Get-RingMasterNodeAcl -Session $session -Path "/Foo"
````
## Get stat of a node
```` powershell
Get-RingMasterNodeStat -Session $session -Path "/Foo"
````
## Set the data associated with a node
```` powershell
$data = [System.Guid]::NewGuid().ToByteArray()
Set-RingMasterNodeData -Session $session -Path "/Foo" -Data $data
````
## Set the ACL associated with a node
```` powershell
$acls = @(New-RingMasterAcl -Permission Write -Scheme digest -Identifier someid)
Get-RingMasterNodeAcl -Session $session -Path "/Foo" -Acls $acls
````
## Delete a node
```` powershell
Remove-RingMasterNode -Session $session -Path "/Foo"
````
## Move a node
```` powershell
Move-RingMasterNode -Session $session -SourcePath "/Foo" -DestinationPath "/Bar"
````
## Send Multiple operations as a single request
````powershell
$ops = @(New-RingMasterCreateOperation -Path "/Foo1"; New-RingMasterDeleteOperation -Path "/Foo")
Send-RingMasterMultiRequest -Session $session -Operations $ops
````
Or
````powershell
Send-RingMasterBatchRequest -Session $session -Operations $ops
````