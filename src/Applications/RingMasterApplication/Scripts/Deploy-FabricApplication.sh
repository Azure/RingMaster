#!/usr/bin/env bash

cd ${0%/*}
echo Connecting to local cluster.
sfctl cluster select --endpoint http://localhost:19080
echo Uploading RingMaster Application.
sfctl application upload --path RingMaster --show-progress
echo Provisioning RingMaster Application.
sfctl application provision --application-type-build-path RingMaster
echo Creating RingMaster Applciation on cluster.
sfctl application create --app-name fabric:/RingMaster --app-type RingMasterApplication --app-version @BUILDNUMBER@ --parameters @"parameters.json"
