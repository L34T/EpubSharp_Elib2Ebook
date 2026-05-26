#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

dotnet build "${REPO_ROOT}/EpubSharp/EpubSharp.csproj" -c Release -f net9.0
dotnet test "${REPO_ROOT}/EpubSharp.Tests/EpubSharp.Tests.csproj" -c Release
