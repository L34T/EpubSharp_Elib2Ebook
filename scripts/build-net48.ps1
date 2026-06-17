Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath "..")
$dotnet48Dir = Join-Path -Path $repoRoot -ChildPath "dotnet48"

Push-Location $dotnet48Dir
try {
    dotnet build (Join-Path -Path $repoRoot -ChildPath "EpubSharp\EpubSharp.csproj") -c Release -p:NetVersion=4
}
finally {
    Pop-Location
}
