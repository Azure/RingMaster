# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# How To Build

## Prerequisite

Install the latest version of [Git for Windows](https://git-scm.com/download/win) for working with the repo.

Install [Visual Studio 2017](https://www.visualstudio.com/downloads/) with Windows desktop C# and dotnet core support.
Either Professional or Enterprise edition will work, Community edition is not tested.

[NuGet](https://www.nuget.org/downloads) should be already installed with Visual Studio 2017. If you choose to install
MSBuild / .NET SDK / Windows SDK, then install the command line version. NuGet is required to restore several packages
before the build.

On Linux machine, install Dotnet Core SDK 2.1 or more recent version and download the latest nuget. To run nuget,
install latest stable version of Mono from [official website](http://www.mono-project.com).

## Bootstrap the development environment

In Start Menu (or whatever equivalent), find "Visual Studio 2017", open "visual Studio Tools" folder, click "Developer
Command Prompt for VS 2017". A command prompt will show up, where one may run MSBuild, C# and C++ compilers. Then set
the environment variable OSSBUILD to 1.

## Build on Windows platform

To build all projects, simply run `build\build.cmd` in a Command Prompt after cleaning up the workspace.

To open project in Visual Studio IDE, assuming the workspace is stored at `C:\rd\Networking-Vega`, after openning
"Developer Command Prompt for VS 2017" from Start Menu, run the following command to generate the solution file:

    cd C:\rd\Networking-Vega\src
    set SRCROOT=C:\rd\Networking-Vega\src
    powershell ..\ossbuild\proj2sln.ps1 -Sln .\src.sln dirs.proj

Then open `src.sln` in file explorer or command prompt.  Note that the conversion from project files to solution file
can be performed to any `*.csproj` files.

If any NuGet package is not restored automatically during the build, run the following at `src` directory:

    nuget restore packages.config
    msbuild /v:m /t:restore

Recommended way to run MSBuild is:

    msbuild /v:m /m /fl

Which means:

* Verbosity level for console output is minimal.
* Use all available processors to build projects in parallel.
* Save detailed build log in msbuild.log.

Building inside Visual Studio or using dotnet CLI also works.

## Build on Linux platform

On Linux only dotnet CLI is supported, use the following steps to build the code:

    export OSSBUILD=1
    cd src
    nuget restore packages.config
    dotnet build

