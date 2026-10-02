# How To Build RingMaster On Windows And Deploy To Linux

---

## Microsoft Internal Repo

### Prerequisites

#### Windows

Install [Visual Studio 2017](https://www.visualstudio.com/downloads/) with the following options. Either Professional or Enterprise edition will work, Community edition is not tested.
* Windows -> .NET desktop development
* Other Toolsets -> .NET Core cross-platform development

#### Linux

Install [.NET SDK](https://www.microsoft.com/net/learn/get-started/linux/ubuntu16-04).  
Install [Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started-linux#set-up-a-local-cluster).

### How to Build On Windows

1. Remove the line `<RuntimeIdentifier>win7-x64</RuntimeIdentifier>` from `src/Applications/RingMasterApplication/RingMasterService/RingMasterService.csproj` and `src/Applications/RingMasterApplication/RingMasterWatchdog/RingMasterWatchdog.csproj`.
   > With the identifier, `dll` binaries are still generated, however it will crash onces deployed to Service Fabric.

2. Refer to `How To Build` section in `README.md`.
   > You can try building using `dotnet build` either in Windows or Linux, however it is not fully supported.
   > 1. Remove all feeds except for public `nuget.org` inside `NuGet.config`.
   >    * Remove line `<add key="Manual" value="https://msazure.pkgs.visualstudio.com/_packaging/ManualMirror/nuget/v3/index.json" />`.
   >    * Remove line `<add key="OSS" value="https://msazure.pkgs.visualstudio.com/_packaging/OSS/nuget/v3/index.json" />`.
   >    * Remove line `<add key="Toolset" value="https://msazure.pkgs.visualstudio.com/_packaging/Toolset/nuget/v3/index.json" />`.
   >    * Remove line `<add key="Nuget" value="https://msazure.pkgs.visualstudio.com/_packaging/NugetMirror/nuget/v3/index.json" />`.
   >    * Remove line `<add key="Official" value="https://msazure.pkgs.visualstudio.com/_packaging/Official/nuget/v3/index.json" />`.
   >    * Remove line `<add key="MSNugetMirror" value="https://msazure.pkgs.visualstudio.com/_packaging/MSNugetMirror/nuget/v3/index.json" />`.
   >    * Remove line `<add key="Geneva" value="https://msblox.pkgs.visualstudio.com/_packaging/AzureGenevaMonitoring/nuget/v3/index.json" />`.
   >    * Remove line `<add key="CBT" value="https://www.myget.org/F/cbt/api/v3/index.json" />`.
   > 2. Use `dotnet build` within a project directory (where **csproj** file exists).
   >    * Projects requiring Windows only packages will not build.

3. Change log folder path inside `appSettings.json` to valid Linux path's. Change directory to `out` folder and use the following command.

   ```bash
   find . -iname "appSettings.json" -exec sed -i 's/c:\\\\Resources\\\\Directory/\/home\/<User>\/Resources\/Directory/' '{}' \;
   ```

### How to Deploy to Linux

#### Running Programs and Unittests

1. When you build the project, nuget packages are downloaded for the projects that required them.
   By default the packages are located inside:
   * `C:\Users\<User Name>\.nuget\packages`.
   * `C:\Program Files\dotnet\sdk\NuGetFallbackFolder`.  

   Copy the packages in both the path's to some location on the Linux machine.

2. Copy the directory where the binaries are generated to some location on the Linux machine.
   By default they are generated at: `C:\rd\Networking\Vega\out`.

3. Most of the projects will generate a `*.runtimeconfig.dev.json` file, where the binary `dll` files are located.
   The file will look something like the following:

   ```json
   {
       "runtimeOptions": {
           "additionalProbingPaths": [
               "C:\\Users\\<User Name>\\.nuget\\packages",
               "C:\\Program Files\\dotnet\\sdk\\NuGetFallbackFolder"
           ]
       }
   }
   ```

   The path's inside `additionalProbingPaths` point to where the nuget packages are stored, so for Linux
   change the path's to where the package folders were copied. Change directory to `out` folder and use the following command.

   ```bash
   find . -iname "*runtimeconfig.dev.json" -exec sed -i 's/C:\\\\Users\\\\<User Name>\\\\.nuget\\\\packages/\/home\/<User>\/Vega\/nuget_packages/' '{}' \;
   find . -iname "*runtimeconfig.dev.json" -exec sed -i 's/C:\\\\Users\\\\Program Files\\\\dotnet\\\\sdk\\\\NuGetFallbackFolder/\/home\/<User>\/Vega\/nugetfallbackfolder_packages/' '{}' \;
   ```

4. Now you should be able to run any program or unittests with the following command:

   ```bash
   dotnet <Name of Program>.dll
   dotnet vstest <Name of Unittest>.dll
   ```

#### RingMaster Application

1. Copy the directory where the RingMaster Application package is generated to some location on the Linux machine.
   By default the package is generated at: `C:\rd\Networking\Vega\out\Release-x64\RingMasterApplication-Pkg`.

2. Rename the folder `RingMasterApplication` inside `RingMasterApplication-Pkg` to `RingMaster`.

   ```bash
   mv RingMasterApplication-Pkg/RingMasterApplication RingMasterApplication-Pkg/RingMaster
   ```

3. Change the entry point of both `RingMasterService` and `RingMasterWatchdog`
   to the `entryPoint.sh` scripts.
   * For `RingMasterService` change the line `<Program>Microsoft.RingMaster.RingMasterService.exe</Program>` inside `RingMasterApplication-Pkg/RingMaster/RingMasterWatchdog/ServiceManifest.xml`
     to `<Program>entryPoint.sh</Program>`.
   * For `RingMasterWatchdog` change the line `<Program>Microsoft.RingMaster.RingMasterWatchdog.exe</Program>` inside `RingMasterApplication-Pkg/RingMaster/RingMasterWatchdog/ServiceManifest.xml`
     to `<Program>entryPoint.sh</Program>`.  
     
   Change to any directory which incompasses both files mentioned above. E.g. `RingMasterApplication-Pkg/RingMaster` and then run the following command:
   ```bash
   find . -iname "ServiceManifest.xml" -exec sed -i 's/Microsoft\.RingMaster\.RingMasterService\.exe|Microsoft\.RingMaster\.RingMasterWatchdog\.exe/entryPoint.sh/' '{}' \;
   ```

4. We need to make sure the files read by Service Fabric have Linux line endings instead of Windows.
   There is a utility `dos2unix`, which does exactly that. Install it via the following command:

   ```bash
   sudo apt install dos2unix
   ```

   Then, use the following commands to convert files to Unix line endings.

   ```bash
   dos2unix RingMasterApplication-Pkg/parameters.json
   dos2unix RingMasterApplication-Pkg/Deploy-FabricApplication.sh
   dos2unix RingMasterApplication-Pkg/Remove-FabricApplication.sh
   dos2unix RingMasterApplication-Pkg/RingMaster/ApplicationManifest.xml
   dos2unix RingMasterApplication-Pkg/RingMaster/RingMasterService/ServiceManifest.xml
   dos2unix RingMasterApplication-Pkg/RingMaster/RingMasterService/Config/Settings.xml
   dos2unix RingMasterApplication-Pkg/RingMaster/RingMasterService/Code/entryPoint.sh
   dos2unix RingMasterApplication-Pkg/RingMaster/RingMasterWatchdog/ServiceManifest.xml
   dos2unix RingMasterApplication-Pkg/RingMaster/RingMasterWatchdog/Config/Settings.xml
   dos2unix RingMasterApplication-Pkg/RingMaster/RingMasterWatchdog/Code/entryPoint.sh
   ```

5. Start up Service Fabric service. It will take a few minutes.

   ```bash
   sudo /opt/microsoft/sdk/servicefabric/common/clustersetup/devclustersetup.sh
   ```

   * Use the following command to stop Service Fabric cluster:

     ```bash
     sudo /opt/microsoft/sdk/servicefabric/common/clustersetup/devclustercleanup.sh
     ```

6. Install RingMaster Application using the given `Deploy-FabricApplication.sh` script.

   ```bash
   ./RingMasterApplication-Pkg/Deploy-FabricApplication.sh
   ```

   * To uninstall the Application, use the given `Remove-FabricApplication.sh` script.

     ```bash
     ./RingMasterApplication-Pkg/Remove-FabricApplication.sh
     ```
