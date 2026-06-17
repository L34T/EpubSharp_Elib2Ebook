#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

echo "--- Building Legacy (.NET 4.8) ---"
"${SCRIPT_DIR}/build-net48.sh"

echo "--- Building Standard (netstandard2.0) ---"
"${SCRIPT_DIR}/build-net2.sh"

echo "--- Building .NET 9 ---"
"${SCRIPT_DIR}/build-net9.sh"

echo "--- Building .NET 10 (Default) ---"
"${SCRIPT_DIR}/build-net10.sh"
