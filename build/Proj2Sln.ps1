<#
.SYNOPSIS
    Generates a Visual Solution file from one or more cs/cpp project files

.PARAMETER ProjFiles
    Specifies the list of proj files to parse

.PARAMETER SlnFile
    Specifies the output solution file to write. This should be in the current directory

.PARAMETER Exclude
    Specifies the regex pattern to exclude certain project files.

.EXAMPLE
    PS C:\src\RingMaster\src> ..\ossbuild\Proj2Sln.ps1 -ProjFiles .\dirs.proj -SlnFile .\dir.sln
    Read the directory traversal proj file and write a SLN file. Be sure to define SRCROOT environment variable prior
    to this command so the proj files can be resolved.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateScript({ $_ -ne $null -and $_.Length -gt 0 })]
    [string[]] $ProjFiles = "..\Backend\HelperTypes\unittest\HelperTypesUnitTest.csproj",

    [Parameter(Mandatory = $true)]
    [string] $SlnFile = "test.sln",

    [string] $Exclude = "sfproj|nupkg"
    )

Set-StrictMode -Version latest

# Well-known project Guids
$csProjType = '{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}'
$cppProjType = '{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}'
$nuprojType = '{FF286327-C783-4F7A-AB73-9BCBAD0D4460}'
$wcfProjType = '{3D9AD99F-2412-4246-B90B-4EAA41C64699}'
$testProjType = '{3AC096D0-A1C2-E12C-1390-A8335801FDAB}'
$folderType = '{2150E333-8FDC-42A3-9474-1A3956D46DE8}'

# Proj files parsed so far
$files = @{}

# Mapping from project GUID to: name, path, type GUID
$projects = @{}

# Mapping from top-level folder name to a list of project guids under the path
$folders = @{}

function ExpandEnvVariable($s)
{
    $s = $s -Replace "\`$\((.*)\)", "`${env:`$1}"
    $ExecutionContext.InvokeCommand.ExpandString($s)
}

function ParseProjFile($projFile)
{
    if ($files.Contains($projFile) -or !(Test-Path -PathType Leaf $projFile) -or ($projFile -match $Exclude)) {
        return
    }
    else {
        $files[$projFile] = 1
    }

    $projXml = [XML] (Get-Content $projFile)

    $projGuid = $null
    $projType = $null
    
    if ($projXml.Project | Get-Member PropertyGroup) {
        $projXml.Project.PropertyGroup | % {
            $_.ChildNodes | % {
                if ($_.Name -eq "ProjectGuid") {
                    $projGuid = $_.InnerText
                }
                elseif ($_.Name -eq "ProjectTypeGuids") {
                    # Use file extension to determine the type for now
                    # $projType = $_.InnerText
                }
            }
        }
    }

    # New project format, e.g. dotnet core
    if ($projGuid -eq $null -and ($projXml.Project | Get-Member -Name Sdk)) {
        $projGuid = [Guid]::NewGuid()
    }

    if ($projGuid -ne $null) {
        if ($projType -eq $null) {
            $ext = [IO.Path]::GetExtension($projFile)
            $projType = switch ($ext) {
                ".csproj" { $csProjType }
                ".vcxproj" { $cppProjType }
                ".nuproj" { $nuprojType }
                default { $csProjType }
            }
        }

        # Normalize the proj guid
        $projGuid = ([Guid]$projGuid).ToString('B').ToUpper()

        # Normal the type
        $projType = $projType.Split(';')[0]

        $name = [IO.Path]::GetFileNameWithoutExtension($projFile)
        $projects[$projGuid] = @($name, $projFile, $projType)

        Write-Verbose "$name : $projGuid : $projType"
    }

    if ($projXml.Project | Get-Member -Name ItemGroup) {
        $projXml.Project.ItemGroup | % {
            $_.ChildNodes | % {
                if ($_.OuterXml.StartsWith("<ProjectReference") -or $_.OuterXml.StartsWith("<ProjectFile")) {
                    $name = ExpandEnvVariable $_.Include
                    if ($name -notmatch "^[a-z]:\\") {
                        $name = Join-Path ([IO.Path]::GetDirectoryName($projFile)) $name
                    }

                    Write-Verbose "Resolving $name"
                    $name = Resolve-Path -Relative $name -ErrorAction Ignore
                    if (-not ([string]::IsNullOrEmpty($name))) {
                        Write-Verbose "checking $name"
                        ParseProjFile $name
                    }
                }
            }
        }
    }
}

function GenerateFolders
{
    $projects.Keys | % {
        $guid = $_
        $path = $projects[$guid][1]
        
        if ($path.StartsWith(".\")) {
            $path = $path.Substring(2)
        }

        if ($path -match "^\.\." -or $path -match "^[a-z]:") {
            return
        }

        $top = $path.Split('\')[0]
        if ($top -eq $path) {
            return
        }

        if (-not ($folders.Contains($top))) {
            $folders[$top] = New-Object PSObject -Property @{
                GUID = [Guid]::NewGuid().ToString('B').ToUpper()
                Projects = @()
            }
        }
        
        $folders[$top].Projects += $guid
    }
}

#######################################################################################################################

foreach ($projFile in $ProjFiles) {
    if (Test-Path $projFile) {
        ParseProjFile (Resolve-Path -relative $projFile)
    }
}

GenerateFolders

$content = @()
$content += ""
$content += "Microsoft Visual Studio Solution File, Format Version 12.00"
$content += "# Visual Studio 15"
$content += "VisualStudioVersion = 15.0.27428.2037"
$content += "MinimumVisualStudioVersion = 10.0.40219.1"

$projects.Keys | % {
    $guid = $_
    $val = $projects[$guid]

    $content += "Project(`"$($val[2])`") = `"$($val[0])`", `"$($val[1])`", `"$guid`""
    $content += "EndProject"
}

$folders.Keys | % {
    $name = $_
    $guid = $folders[$name].GUID
    $content += "Project(`"$folderType`") = `"$name`", `"$name`", `"$guid`""
    $content += "EndProject"
}

$content += "Global
`tGlobalSection(SolutionConfigurationPlatforms) = preSolution
`t`tDebug|x64 = Debug|x64
`t`tRelease|x64 = Release|x64
`tEndGlobalSection
`tGlobalSection(ProjectConfigurationPlatforms) = postSolution"

$projects.Keys | % {
    $guid = $_

    $content += "`t`t${guid}.Debug|x64.ActiveCfg = Debug|x64"
    $content += "`t`t${guid}.Debug|x64.Build.0 = Debug|x64"
    $content += "`t`t${guid}.Release|x64.ActiveCfg = Release|x64"
    $content += "`t`t${guid}.Release|x64.Build.0 = Release|x64"
}

$content += "`tEndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection"

$content += "`tGlobalSection(NestedProjects) = preSolution"
$folders.Values | % {
    $folderGuid = $_.GUID
    $_.Projects | % {
        $guid = $_
        $content += "`t`t$guid = $folderGuid"
    }
}
$content += "    EndGlobalSection"

$slnGuid = [Guid]::NewGuid().ToString('B').ToUpper()
$content += "`tGlobalSection(ExtensibilityGlobals) = postSolution
`t`tSolutionGuid = $slnGuid
`tEndGlobalSection
EndGlobal"

$content | Out-File -Encoding utf8 -Force -FilePath (Join-Path $pwd $SlnFile)
