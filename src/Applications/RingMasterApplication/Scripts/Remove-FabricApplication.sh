#!/usr/bin/env bash

cd ${0%/*}
echo Connecting to local cluster.
sfctl cluster select --endpoint http://localhost:19080
echo Deleting RingMaster Application.
sfctl application delete --application-id RingMaster
echo Unprovisioning RingMaster Application.
sfctl application unprovision --application-type-name RingMasterApplication --application-type-version @BUILDNUMBER@
echo Deleting RingMaster Application from cluster store.
sfctl store delete --content-path RingMaster
