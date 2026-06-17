Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "--- Building Legacy (.NET 4.8) ---"
& (Join-Path -Path $PSScriptRoot -ChildPath "build-net48.ps1")

Write-Host "--- Building Standard (netstandard2.0) ---"
& (Join-Path -Path $PSScriptRoot -ChildPath "build-net2.ps1")

Write-Host "--- Building .NET 9 ---"
& (Join-Path -Path $PSScriptRoot -ChildPath "build-net9.ps1")

Write-Host "--- Building .NET 10 (Default) ---"
& (Join-Path -Path $PSScriptRoot -ChildPath "build-net10.ps1")
