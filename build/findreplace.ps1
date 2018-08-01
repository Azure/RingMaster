param(
    [string] $Filename,
    [string] $Find,
    [string] $Replace)
$content = [System.IO.File]::ReadAllText($Filename)
$content = $content.Replace($Find, $Replace)
[System.IO.File]::WriteAllText($Filename, $content)
