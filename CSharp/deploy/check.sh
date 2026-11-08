#!/usr/bin/env bash
# Checks that Bold for C# deploys: it packs the libraries, the attribute generator and the bold-codegen tool into
# CSharp/artifacts/packages, installs the tool from there and generates the classes of the smoke test's model, builds
# and runs the package smoke test (deploy/PackageSmoke) on those packages alone, and publishes the programs into
# CSharp/artifacts/publish, starting each server from its published folder and asking it for /health. On Windows it
# also builds and starts the user guide's WinForms window (deploy/WinFormsSmoke) on the packages.
#
#   bash CSharp/deploy/check.sh [--no-build] [--offline] [--source <feed>] [-- <MSBuild arguments>]
#
#   --no-build      pack what dotnet build already built (Release), as CI does after its build
#   --offline       for machines without nuget.org: builds with -p:BoldOffline=true (no service hosting, no SQL
#                   providers in the remote server, the tests' native SQLite in the smoke test)
#   --source <feed> restore the smoke test from this feed besides the Bold packages, instead of nuget.org
#
# On Windows everything is packed and published, the WinForms library and programs too; elsewhere what does not need
# Windows. Run it from anywhere; it works in the repository's CSharp folder.
set -euo pipefail

cd "$(dirname "$0")/.."
root=$(pwd)
artifacts="$root/artifacts"
nobuild=()
offline=false
sources=()
msbuild=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-build) nobuild=(--no-build) ;;
    --offline) offline=true ;;
    --source) sources+=("$2"); shift ;;
    --) shift; msbuild+=("$@"); break ;;
    *) echo "Unknown argument $1" >&2; exit 2 ;;
  esac
  shift
done
if $offline; then
  msbuild+=("-p:BoldOffline=true" "-p:NuGetAudit=false")
fi
windows=false
if [[ "${OSTYPE:-}" == msys* || "${OSTYPE:-}" == cygwin* || "${OS:-}" == "Windows_NT" ]]; then
  windows=true
fi

# A folder's path as the programs take it (D:/a/... on Windows).
native() { if $windows; then (cd "$1" && pwd -W); else (cd "$1" && pwd); fi; }

step() { echo; echo "== $*"; }

# Runs a dotnet command quietly; shows its output when it fails, and the lines matching $2 when it succeeds.
quiet() {
  local name=$1 pattern=$2; shift 2
  local log="$artifacts/check/$name.log"
  if ! "$@" > "$log" 2>&1; then
    cat "$log"; echo "FAIL $name"; return 1
  fi
  grep -E -e "$pattern" "$log" || true
}

rm -rf "$artifacts/packages" "$artifacts/tools" "$artifacts/smoke" "$artifacts/publish" "$artifacts/check"
mkdir -p "$artifacts/packages" "$artifacts/check"

step "Packages"
if $windows; then
  quiet pack "Successfully created" dotnet pack Bold.slnx -c Release ${nobuild[@]+"${nobuild[@]}"} --nologo ${msbuild[@]+"${msbuild[@]}"}
else
  for project in src/*/*.csproj tools/Bold.CodeGen.Tool/Bold.CodeGen.Tool.csproj; do
    if grep -q "net10.0-windows" "$project"; then
      echo "(not on this system: $project)"
      continue
    fi
    quiet "pack-$(basename "$project" .csproj)" "Successfully created" \
      dotnet pack "$project" -c Release ${nobuild[@]+"${nobuild[@]}"} --nologo ${msbuild[@]+"${msbuild[@]}"}
  done
fi
version=$(ls "$artifacts/packages" | sed -nE 's/^Bold\.Core\.(.+)\.nupkg$/\1/p')
echo "$(ls "$artifacts/packages"/*.nupkg | wc -l | tr -d ' ') packages, version $version"

step "bold-codegen, installed from the packages"
dotnet tool install bold-codegen --version "$version" --tool-path "$artifacts/tools" --add-source "$artifacts/packages"
"$artifacts/tools/bold-codegen" deploy/PackageSmoke/Library.bld --out "$artifacts/smoke/generated" --quiet
ls "$artifacts/smoke/generated"

step "The package smoke test, on the packages alone"
restore=(--packages "$artifacts/smoke/packages")
if [[ ${#sources[@]} -gt 0 ]]; then
  restore+=(--source "$artifacts/packages")
  for source in "${sources[@]}"; do restore+=(--source "$source"); done
fi
quiet smoke-restore "Restored" dotnet restore deploy/PackageSmoke "${restore[@]}" -p:BoldVersion="$version" ${msbuild[@]+"${msbuild[@]}"}
dotnet run --project deploy/PackageSmoke -c Release --no-restore -p:BoldVersion="$version" ${msbuild[@]+"${msbuild[@]}"}
if $windows; then
  step "The user guide's WinForms window, on the packages alone"
  quiet winforms-restore "Restored" dotnet restore deploy/WinFormsSmoke "${restore[@]}" -p:BoldVersion="$version" ${msbuild[@]+"${msbuild[@]}"}
  quiet winforms-build "WinFormsSmoke ->" dotnet build deploy/WinFormsSmoke -c Release --no-restore -o "$artifacts/smoke/winforms" -p:BoldVersion="$version" ${msbuild[@]+"${msbuild[@]}"}
  if timeout 120 "$artifacts/smoke/winforms/WinFormsSmoke.exe" --close; then
    echo "ok   the window opens, shows the author in its grid, and closes"
  else
    echo "FAIL the WinForms window (exit code $?)"; exit 1
  fi
fi

step "Programs, published"
publish() {
  quiet "publish-$2" "-> .*publish" dotnet publish "$1" -c Release -o "$artifacts/publish/$2" --nologo ${msbuild[@]+"${msbuild[@]}"}
}
publish src/Bold.Propagator.Server propagator
publish src/Bold.Remote.Server remote
if $windows; then
  publish src/Bold.ModelEditor model-editor
  publish src/Bold.PropagatorAnalyzer propagator-analyzer
fi

# Starts a published server, waits until /health answers and the propagator hub negotiates, and stops it.
probe() {
  local name=$1 dll=$2; shift 2
  local port=$((20000 + RANDOM % 20000))
  local log="$artifacts/check/$name.log"
  dotnet "$dll" --urls "http://127.0.0.1:$port" "$@" > "$log" 2>&1 &
  local pid=$!
  local healthy=false negotiated=false
  for _ in $(seq 1 60); do
    if curl -fsS "http://127.0.0.1:$port/health" > /dev/null 2>&1; then healthy=true; break; fi
    if ! kill -0 "$pid" 2> /dev/null; then break; fi
    sleep 1
  done
  if $healthy && curl -fsS -X POST "http://127.0.0.1:$port/propagator/negotiate?negotiateVersion=1" 2> /dev/null | grep -q connectionId; then
    negotiated=true
  fi
  kill "$pid" 2> /dev/null || true
  wait "$pid" 2> /dev/null || true
  if $healthy && $negotiated; then
    echo "ok   $name answers /health and /propagator from its published folder"
  else
    cat "$log"; echo "FAIL $name (healthy: $healthy, hub: $negotiated)"; return 1
  fi
}

step "Published programs, started"
probe propagator "$artifacts/publish/propagator/Bold.Propagator.Server.dll"
if $offline; then
  echo "(skipped offline: the remote server is published without its SQL providers then)"
else
  data=$(native "$artifacts/check")
  model=$(native deploy/PackageSmoke)/Library.bld
  probe remote "$artifacts/publish/remote/Bold.Remote.Server.dll" \
    --Bold:Model="$model" --Bold:Provider=Microsoft.Data.Sqlite --Bold:ConnectionString="Data Source=$data/remote.db" \
    --Bold:Engine=dbeSQLite --Bold:CreateSchema=true --Bold:Propagator=true
fi
if $windows; then
  "$artifacts/publish/model-editor/Bold.ModelEditor.exe" "$(native deploy/PackageSmoke)/Library.bld" &
  pid=$!
  sleep 10
  if kill -0 "$pid" 2> /dev/null; then
    kill "$pid" 2> /dev/null || true
    echo "ok   the model editor starts from its published folder"
  else
    echo "FAIL the model editor ended right after it started"; exit 1
  fi
fi

echo
echo "Deployable: packages in artifacts/packages, programs in artifacts/publish."
