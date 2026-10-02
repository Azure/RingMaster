# RingMaster

RingMaster is a reliable hierarchical key-value store with change notification. This repository contains the common
libraries, backend core, client tools, and Service Fabric applications.

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License
Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution.
For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate
the pull request appropriately. Follow the instructions provided by the bot. You only need to do this once across all
repositories using the CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com).

## Prerequisites

The following products must be installed on a Windows development machine:

* Visual Studio 2019.
* .NET Core SDK 2.0 or newer.
* Service Fabric SDK 6.2 or newer.

On Linux, install .NET Core SDK 2.1 or newer and download the latest NuGet client. To run NuGet, install the latest
stable version of Mono from the [official website](http://www.mono-project.com).

Azure DevOps NuGet feeds require authentication. Builds that use those feeds require MSBuild 15.8 or newer and the
NuGet credential provider. Install the credential provider with the `-AddNetfx` option by using the script at:

    https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.ps1

See the [manual installation instructions](https://github.com/Microsoft/artifacts-credprovider#manual-installation-windows)
for other installation options.

## How to Build

### Windows

To build all projects, run `build\build.cmd` in a command prompt after cleaning the workspace.

To generate a Visual Studio solution, assuming the workspace is stored at `C:\rd\RingMaster`, open a Developer Command
Prompt for Visual Studio 2019 and run:

```powershell
cd C:\rd\RingMaster\src
set SRCROOT=C:\rd\RingMaster\src
powershell
PS C:\rd\RingMaster\src> ..\build\proj2sln.ps1 -Sln .\src.sln dirs.proj
```

Then open `src.sln`. The conversion can also be performed for individual `*.csproj` files.

If NuGet packages are not restored automatically, run the following commands from the `src` directory:

    nuget restore packages.config
    msbuild /v:m /t:restore

The recommended MSBuild command is:

    msbuild /v:m /m /fl

This uses minimal console verbosity, builds projects in parallel, and writes a detailed log to `msbuild.log`.
Building inside Visual Studio or using the .NET CLI also works.

### Public environment

Outside Microsoft corpnet, internal NuGet feeds and instrumentation packages are unavailable. Set the following
environment variable before invoking MSBuild:

    set OSSBUILD=1

### Linux

On Linux, only the .NET CLI is supported:

    export OSSBUILD=1
    cd src
    nuget restore packages.config
    dotnet build
