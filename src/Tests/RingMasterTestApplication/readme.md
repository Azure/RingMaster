RingMasterTestApplication Usage
==================================================

Some stress tests in the RingMasterTestApplication assume certain nodes exists so they can call get/set data on the node or its children. For example:

1. ControlPlaneStress service assumes that a node called /Data (parameter "ControlPlaneStress.TestPath" in ApplicationManifest.xml) exists with many descendants (typically 100,000). It enumerates the descendants of /Data when it starts up and then randomly issues SetData requests for one of the children. So the /Data node must be created before this service is started.
2. ServingPlaneStress service also looks for a node called /Data (parameter "ServingPlaneStress.TestPath" in ApplicationManifest.xml) - it loads all the descendants of that node and then issues GetData requests randomly.
3. EnumerationStress looks for a node called /Flat10M (parameter "EnumerationStress.TestPath" in ApplicationManifest.xml) which must be created prior to the service being started.
4. ConnectionStree service also looks for a node called /Data (parameter "ConnectionStress.TestPath" in ApplicationManifest.xml).

The typical stress setup will contain

- /Data with 100,000 descendants
- /Flat10M with 10M direct children of the node (doesn't have to be 10M can be 100K too)

You can either create these nodes before starting RingMasterTestApplication or change these settings to point to other nodes that you have populated already