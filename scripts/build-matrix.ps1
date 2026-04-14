Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

& (Join-Path -Path $PSScriptRoot -ChildPath "build-net9.ps1")
& (Join-Path -Path $PSScriptRoot -ChildPath "build-net10.ps1")
