param(
    [parameter(Mandatory=$false)]
    $buildFlavor = 'retail-amd64'
)

Push-Location -Path "$env:OUTPUTROOT\$buildFlavor"

[xml]$manifest = Get-Content -Path ".\CloudUnitTests\ValidationDescription.xml"

$testCount = 0;
foreach ($taskDescription in $manifest.ValidationDescription.TaskGroup.ValidationReportTaskDescriptions)
{
  $command = [string]$taskDescription.TaskCommand.InnerText
  cmd /c $command
  if ($lastExitCode -ne 0)
  {
      Pop-Location
      exit $lastExitCode
  }
  $testCount += 1
}

Write-Host "Total number of tests: $testCount"
Pop-Location