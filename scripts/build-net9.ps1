Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath "..")
$dotnet9Dir = Join-Path -Path $repoRoot -ChildPath "dotnet9"

Push-Location $dotnet9Dir
try {
    dotnet build (Join-Path -Path $repoRoot -ChildPath "EpubSharp\\EpubSharp.csproj") -c Release -p:NetVersion=9
    dotnet test (Join-Path -Path $repoRoot -ChildPath "EpubSharp.Tests\\EpubSharp.Tests.csproj") -c Release -p:NetVersion=9
}
finally {
    Pop-Location
}
