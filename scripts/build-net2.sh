#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

cd "${REPO_ROOT}/dotnet2"
dotnet build "${REPO_ROOT}/EpubSharp/EpubSharp.csproj" -c Release -p:NetVersion=2
dotnet test "${REPO_ROOT}/EpubSharp.Tests/EpubSharp.Tests.csproj" -c Release -p:NetVersion=2
