Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath "..")

dotnet build (Join-Path -Path $repoRoot -ChildPath "EpubSharp\\EpubSharp.csproj") -c Release -f net9.0
dotnet test (Join-Path -Path $repoRoot -ChildPath "EpubSharp.Tests\\EpubSharp.Tests.csproj") -c Release
