# How To Build RingMaster

## Public Repo on GitHub

### Prerequisite

Install the latest version of [Git for Windows](https://git-scm.com/download/win) for working with the repo.

Install [Visual Studio 2019](https://www.visualstudio.com/downloads/) with Windows desktop C# and C++ support.
Either Professional or Enterprise edition will work, Community edition is not tested.

[NuGet](https://www.nuget.org/downloads) should be already installed with Visual Studio 2019. If you choose to install
MSBuild / .NET SDK / Windows SDK, then install the command line version. NuGet is required to restore several packages
before the build.

### Bootstrap the development environment

In Start Menu (or whatever equivalent), find "Visual Studio 2019", open "visual Studio Tools" folder, click "Developer
Command Prompt for VS 2019". A command prompt will show up, where one may run MSBuild, C# and C++ compilers.

Change to the `ossbuild` directory in the repo, for instance `C:\rd\Networking\Vega\ossbuild`, start PowerShell, and run
`ossbuild.ps1`. The script will restore all required packages, generate a file for package definitions, and set several
environment variables.

### Build the source code

Go to any directory at root, `src`, or under `src`, run MSBuild like what you normally do. The binaries are saved at
`out` directory under the repo root.

The projects are designed to be built in parallel. If the number of processor is 8 (check the environment variable
`NUMBER_OF_PROCESSOR`), the recommended command to build is:

    msbuild /m:8 /v:m

The second argument sets the verbosity to minimal.

### Build in Visual Studio IDE

Once in the bootstrapped PowerShell window, one can open any project in VS IDE, for instance:

    devenv .\Backend\Common\RingMasterBackendCommon.csproj

Because the VS solution file (`*.sln`) is not checked in, you may need to manually add depended projects to the default
solution in order to *rebuild*.  If you build once in the command line using MSBuild, incremental build will work.  You
may also write a script to parse all csproj files and generate a sln file.

## Microsoft Internal Repo

### Prerequisite

Follow the instruction provided by Azure engineering system team.

### Bootstrap the development environment

Change to the root directory of the repo, open a command prompt, and run `init.cmd`.

### Build the source code

Go to any directory at `src` or under, run `build` command to build that project as well as all dependencies.

### Build in Visual Studio IDE

Once in the bootstrapped command prompt, one can open any project *and* its dependencies using `vsmsbuild`, such as:

    cd c:\rd\Networking\Vega\src
    vsmsbuild dir.proj
