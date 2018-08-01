#!/usr/bin/env bash
# Run all unit tests in Networking-Vega. Used by CDPx pipeline.

cd ${0%/*}

subdir=../out/$1
if [ "$subdir" = "../out/" ]; then
   subdir=${subdir}Release-x64
fi

sleeptime=$2
if [ "$sleeptime" = "" ]; then
   sleeptime=15
fi

dn='dotnet vstest'
export TestEnvironment=QTEST

utlist=(CommunicationProtocolUnitTest/Microsoft.RingMaster.CommunicationProtocolUnitTest
        HelperTypesUnitTest/Microsoft.RingMaster.HelperTypesUnitTest
        LogStreamUnitTest/Microsoft.RingMaster.LogStreamUnitTest
        RingMasterBackendCoreUnitTest/Microsoft.RingMaster.Backend.CoreUnitTest
        RingMasterBackendNativeUnitTest/Microsoft.RingMaster.Backend.SortedDictExtUnitTest
        RingMasterClientUnitTest/Microsoft.RingMaster.ClientUnitTest
        RingMasterCommonUnitTest/Microsoft.RingMaster.CommonUnitTest
        SecureTransportUnitTest/Microsoft.RingMaster.SecureTransportUnitTest
        ServiceFabricUnitTest/Microsoft.RingMaster.ServiceFabricUnitTest
        RingMasterBVT/Microsoft.RingMaster.Test.BVT
        EndToEndTests/Microsoft.RingMaster.Test.EndToEnd)

for ut in ${utlist[@]}; do
  $dn $subdir/${ut}.dll "--logger:trx"

  if [ "$?" -ne 0 ]; then
    exit $?
  fi

  sleep $sleeptime
done
