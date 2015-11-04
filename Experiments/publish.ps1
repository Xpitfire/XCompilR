Param (
    $variables = @{},   
    $artifacts = @{},
    $scriptPath,
    $buildFolder,
    $srcFolder,
    $outFolder,
    $tempFolder,
    $projectName,
    $projectVersion,
    $projectBuildNumber
)

Write-Output "Publishing NuGet package"

# get script variables
$nugetApiKey = $variables["NuGetApiKey"]

$nuspecFiles = Get-ChildItem $srcFolder -include -recurse *.nuspec
foreach($file in $nuspecFiles)
{
	Write-Output $file
}