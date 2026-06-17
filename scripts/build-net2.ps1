Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath "..")
$dotnet2Dir = Join-Path -Path $repoRoot -ChildPath "dotnet2"

Push-Location $dotnet2Dir
try {
    dotnet build (Join-Path -Path $repoRoot -ChildPath "EpubSharp\EpubSharp.csproj") -c Release -p:NetVersion=2
    dotnet test (Join-Path -Path $repoRoot -ChildPath "EpubSharp.Tests\EpubSharp.Tests.csproj") -c Release -p:NetVersion=2
}
finally {
    Pop-Location
}
