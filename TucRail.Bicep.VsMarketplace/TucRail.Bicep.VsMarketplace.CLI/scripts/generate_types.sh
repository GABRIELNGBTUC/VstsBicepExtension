#!/bin/bash
set -e

root="$(dirname ${BASH_SOURCE[0]})/.."

dotnet run --project "$root/" -- --outdir "$root/types"