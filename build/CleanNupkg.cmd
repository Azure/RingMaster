@REM Deletes the temporary files for building nuget packages so code signing won't complain.
@setlocal enabledelayedexpansion

@rem Starting at the enlistment root
cd /d %~dp0
cd ..
cd out\release-x64

rd/s/q ApiCoverageNupkg
rd/s/q RingMasterBackendNupkg
rd/s/q RingMasterClientNupkg
rd/s/q RingMasterCommonNupkg
rd/s/q RingMasterPersistenceNupkg
rd/s/q RingMasterToolsNupkg
rd/s/q ServiceFabricApplicationNupkg
rd/s/q VegaDistributedTestNupkg
