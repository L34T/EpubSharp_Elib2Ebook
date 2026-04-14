Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath "..")
$dotnet10Dir = Join-Path -Path $repoRoot -ChildPath "dotnet10"

Push-Location $dotnet10Dir
try {
    dotnet build (Join-Path -Path $repoRoot -ChildPath "EpubSharp\\EpubSharp.csproj") -c Release -f net10.0 -p:EpubSharpEnableNet10=true
    dotnet test (Join-Path -Path $repoRoot -ChildPath "EpubSharp.Tests\\EpubSharp.Tests.csproj") -c Release
}
finally {
    Pop-Location
}
