# Cloud Test Onbarding Step By Step

This note documents the steps to onboard Networking-Vega repo to Cloud Test. For official doc, refer to [Cloud Test User
Guide](http://aka.ms/cug). This is a "TL;DR" version.

## Prerequisites

See official doc for details. Most important one is the existing repo should have onboarded Cloud Build (i.e. able to use
`buildreq -q` to schedule Q build), and VSTS drop is enabled (i.e. you can see line of `drop get -a -u
https://msazure... -d c:\your_local_folder`).

## Get Cloud Test tool

Run the following nuget command to download the tool:

    nuget install CloudTestClient.OnCorext

You may add it in `.corext\corext.config`:

    <repo name="Bing" uri="https://msasg.pkgs.visualstudio.com/DefaultCollection/_apis/packaging/Bing/nuget/index.json" fallback="http://wanuget/MSNugetMirror/nuget/" />
    ...
    <package id="CloudTestClient.OnCorext" version="1.1.1" />

Assume the tool is installed at `D:\CxCache\CloudTestClient.OnCorext.1.1.1`.

## Write Test Map files

Cloud Test relies on test map XML files to know what the test job looks like, for each test job where to copy files, and
how to run the test cases.  Test map files should be part of the build artifact.  In this repo, all files required by
Cloud Test are checked in at `src\CloudUnitTests` (shared with WAES), and after the build they are saved at
`out\debug-AMD64\CloudUnitTests` directory.

`TestMap.xml` defines test job groups.  There is only one defined in this repo, pointing to `VMTestGroup.xml`.  In the
latter, resource type is provided by Cloud Test team. This is specific to the *tenant* (or the pools of machines) where
the test will be running. "Setup" section contains information about build files to be copied, and batch script to run
prior to the normal test execution phase.

In `VMTestGroup.xml`, several *TestJob* are defined to run test suites using either MSTest or Taef.  It is also possible
to run a custom script and write a WTT result XML file.  This repo mainly relies on Taef and will go back to MSTest
later for dotnet compatibility.

Finally, a batch script in "Cleanup" section is defined to restore the machine to clean state.

## What to write in setup script

This repo uses Windows 2016 image built by One Branch. Other than monitoring agent and VC12 runtime, no other software
is deployed.  In order to deploy Service Fabric 5.6 CU2, we have to download and install VC11 / VC14 runtime. After
installing Service Fabric, a local Service Fabric cluster with 5 replicas is installed, and RingMasterApplication is
deployed.

## How to schedule a Cloud Test job manually

Firstly check in all aforementioned files to local branch, schedule a CloudBuild. When it is finished, note down the
VSTS drop URL in CloudBuild page in the form of `https://msazure.../drop/drops/CDP/Networking-Vega/...`.

Contact Cloud Test team to know which *tenant* to use and which *stamp* it is, let's say it is onebranchtest and
Stamp-CO3, respectively.  The command to schedule a test job is:

```
d:\CxCache\CloudTestClient.OnCorext.1.1.1\tools\ct.cmd -t "[BuildRoot]\[BuildType]-[BuildArch]\CloudUnitTests\TestMap.xml" -tenant onebranchtest -drop https://msazure.artifacts.visualstudio.com/... -cache "True" -type "retail" -arch "amd64" -env "Prod" -stamp "Stamp-CO3" -tcs "None"
```

Note that "-drop" value must be correct, or the build cannot be found. You will see a URL in green to check the progress
and result, and a URL in yellow to cancel the test job.

## How to hold the machine for test failure

Add `-props "HoldTrigger=Failure"` to ct.cmd will cause the machine with test failure being held.  You will receive an
email including IP address, etc.  You should have create a jumpbox in the same VNET of the machine pool to RDP to the
VM.  Read the official doc for more details.

## How to add Cloud Test to merge validation

Once Cloud Test is added to the merge validation, it can be a part of check-in quality gate or separate tests running in
parallel with Cloud Build.  This repo uses the latter, a.k.a. fire-and-forget.

In the existing merge validation XML file (`.config\.cdp\Networking-Vega-MergeValidation.xml`), use the following to add
both CloudBuild and CloudTest:

    <PathUri>src/VSTS/Definitions/MergeValidationWithCloudTest.xml</PathUri>

Parameters submitted to Cloud Test is the following:
```
<Stages>
  <ValidateStage>
    <Enabled>true</Enabled>
    <MainStep>
      <Inputs>
        <SubmitAndWaitUntilSuccess>false</SubmitAndWaitUntilSuccess>
        <SubmitOnly>true</SubmitOnly>
        <Environment>Prod</Environment>
        <BuildSessionId>Dictionary_Build_UniqueSessionId</BuildSessionId>
        <TenantId>onebranchtest</TenantId>
        <TestMapLocation>[BuildRoot]\[BuildType]-[BuildArch]\CloudUnitTests\TestMap.xml</TestMapLocation>
     </Inputs>
    </MainStep>
  </ValidateStage>
</Stages>
```

Above parameters mean Cloud Test job will be submitted after Cloud Build is completed, but the Pull Request will not
wait for Cloud Test completion (fire-and-forget).  For using Cloud Test as a quality gate in PR, remove both
"SubmitOnly" and "SubmitAndWaitUntilSuccess" to take the default behavior.

## Taef compatiblity

Taef on Cloud Test machines are v10, but files in `x64\NetFx4.5` aren't copied to main execution directory to replace
the default ones.  Therefore async test cases like the following are not supported.  One must wrap them in sync test
code or use your own Taef binaries:

```csharp
[TestMethod]
public async void MyTestCase()
{
    ...
}
```

## Contacts

Contact OneBranch Test - Support Team (`onebranchtestsup`) for general info, and CloudTest DRI (`cloudtest-dri`) for
specific info.
