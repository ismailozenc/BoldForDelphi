<#
.SYNOPSIS
Installs Bold for C# on this machine from the repository: the packages, the bold-codegen tool and the programs.

.DESCRIPTION
Packs the libraries, the attribute generator and bold-codegen into CSharp/artifacts/packages, makes that folder a NuGet
package source (so that "dotnet add package Bold.Persistence.Sql" finds the packages), installs bold-codegen as a global
.NET tool, and publishes the programs (the model editor, the propagator analyzer and the two servers) into
CSharp/artifacts/publish. Packages of the same version that an earlier install left in NuGet's cache are removed, so
that applications get the ones just packed.

It needs the .NET 10 SDK and nothing else. On Windows everything is installed; elsewhere what does not need Windows.
Run it again after you pull a new revision. deploy/check.sh is the other script here: it proves the packages work, and
installs nothing.

.PARAMETER Version
The version of the packages; 1.0.0 when not given.

.PARAMETER SourceName
The name of the NuGet package source; "bold" when not given.

.PARAMETER NoBuild
Packs and publishes what "dotnet build CSharp/Bold.slnx -c Release" already built.

.PARAMETER NoPrograms
Leaves the programs out: only the packages, the package source and bold-codegen.

.PARAMETER NoVisualStudio
Leaves Visual Studio alone. Without it, on Windows with Visual Studio 2022 or later, the installer also builds Bold for
Visual Studio (vs/build.ps1) and installs it: a Bold submenu in the Tools menu, .bld files opened in the model editor,
and class generation from Solution Explorer. Close Visual Studio first.

.PARAMETER Uninstall
Removes the package source, the bold-codegen tool, the cached packages, the .bld file type and the Visual Studio
extension. The repository's files stay.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File CSharp\deploy\install.ps1

.EXAMPLE
pwsh CSharp/deploy/install.ps1 -Version 1.2.0 -NoPrograms
#>
[CmdletBinding()]
param(
    [string]$Version = '',
    [string]$SourceName = 'bold',
    [switch]$NoBuild,
    [switch]$NoPrograms,
    [switch]$NoVisualStudio,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$packages = Join-Path $artifacts 'packages'
$publish = Join-Path $artifacts 'publish'
$logs = Join-Path $artifacts 'install'
# Windows PowerShell 5.1 has no $IsWindows; it runs on Windows only.
$onWindows = ($PSVersionTable.PSVersion.Major -lt 6) -or $IsWindows

function Step([string]$text) {
    Write-Host ''
    Write-Host "== $text"
}

# Runs dotnet quietly, its output in a log; shows the log and stops when it fails.
function Invoke-DotNet([string]$name, [string[]]$arguments) {
    $log = Join-Path $logs "$name.log"
    # Windows PowerShell 5.1 makes an error of every line a program writes to its error stream; the exit code decides here.
    $preference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & dotnet @arguments > $log 2>&1
    }
    finally {
        $ErrorActionPreference = $preference
    }
    if ($LASTEXITCODE -ne 0) {
        Get-Content $log | Write-Host
        throw "dotnet $($arguments -join ' ') failed (exit code $LASTEXITCODE); its output is in $log"
    }
}

# Whether dotnet has a package source of this name.
function Test-Source([string]$name) {
    # A source is listed as "  1.  name [Enabled]" with its address on the next line; the word in brackets is translated.
    $detailed = & dotnet nuget list source --format Detailed
    foreach ($line in $detailed) {
        if ($line -match '^\s*\d+\.\s+(.+?)\s+\[[^\]]+\]\s*$' -and $Matches[1] -eq $name) {
            return $true
        }
    }
    return $false
}

# The Bold packages NuGet keeps in its cache: a package packed again under the same version would not replace them.
function Clear-CachedPackages {
    $line = (& dotnet nuget locals global-packages --list) | Select-Object -First 1
    $cache = ($line -replace '^[^:]*:\s*', '').Trim()
    if (-not $cache -or -not (Test-Path $cache)) {
        return
    }
    $removed = 0
    foreach ($folder in Get-ChildItem -Path $cache -Directory) {
        if ($folder.Name -like 'bold.*' -or $folder.Name -eq 'bold-codegen') {
            Remove-Item -Recurse -Force $folder.FullName
            $removed++
        }
    }
    Write-Host "Removed $removed cached Bold packages from $cache"
}

# Uninstalls the bold-codegen global tool when it is installed.
function Remove-Tool {
    $installed = & dotnet tool list --global
    foreach ($line in $installed) {
        if ($line -match '^\s*bold-codegen\s') {
            $preference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                & dotnet tool uninstall --global bold-codegen 2>&1 | Out-Null
            }
            finally {
                $ErrorActionPreference = $preference
            }
            return
        }
    }
}

# Whether Visual Studio (2022 or later, with MSBuild) is on this machine.
function Test-VisualStudio {
    if (-not $onWindows) {
        return $false
    }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        return $false
    }
    $found = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath
    return [bool]$found
}

# Makes .bld the file type of the model editor for this user: a double click in Explorer opens the model in it.
function Set-ModelFileType([string]$editor) {
    $classes = 'HKCU:\Software\Classes'
    New-Item -Path "$classes\.bld" -Force | Out-Null
    Set-ItemProperty -Path "$classes\.bld" -Name '(Default)' -Value 'Bold.Model'
    New-Item -Path "$classes\Bold.Model\shell\open\command" -Force | Out-Null
    Set-ItemProperty -Path "$classes\Bold.Model" -Name '(Default)' -Value 'Bold model'
    Set-ItemProperty -Path "$classes\Bold.Model\shell\open\command" -Name '(Default)' -Value "`"$editor`" `"%1`""
}

function Remove-ModelFileType {
    $classes = 'HKCU:\Software\Classes'
    if ((Test-Path "$classes\.bld") -and ((Get-ItemProperty -Path "$classes\.bld").'(default)' -eq 'Bold.Model')) {
        Remove-Item -Path "$classes\.bld" -Recurse -Force
    }
    if (Test-Path "$classes\Bold.Model") {
        Remove-Item -Path "$classes\Bold.Model" -Recurse -Force
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is not installed (dotnet is not on the path). Install the .NET 10 SDK from https://dotnet.microsoft.com/download and run this again.'
}
Push-Location $root
try {
    $sdk = (& dotnet --version)
    if ($LASTEXITCODE -ne 0 -or -not ($sdk -match '^10\.')) {
        throw "Bold for C# needs the .NET 10 SDK; dotnet --version says '$sdk' here. Install it from https://dotnet.microsoft.com/download and run this again."
    }

    if ($Uninstall) {
        Step 'Uninstalling Bold for C#'
        if (Test-Source $SourceName) {
            & dotnet nuget remove source $SourceName | Write-Host
        }
        else {
            Write-Host "No package source named $SourceName"
        }
        Remove-Tool
        Clear-CachedPackages
        if ($onWindows) {
            Remove-ModelFileType
            if (-not $NoVisualStudio -and (Test-VisualStudio)) {
                & (Join-Path $root 'vs\build.ps1') -Uninstall
            }
        }
        Write-Host ''
        Write-Host 'Uninstalled. The repository and CSharp/artifacts are as they were.'
        return
    }

    New-Item -ItemType Directory -Force -Path $logs | Out-Null
    $common = @('-c', 'Release', '--nologo')
    if ($NoBuild) {
        $common += '--no-build'
    }
    if ($Version) {
        $common += "-p:Version=$Version"
    }

    Step "Packages (.NET SDK $sdk)"
    if (Test-Path $packages) {
        Remove-Item -Recurse -Force $packages
    }
    if ($onWindows) {
        Invoke-DotNet 'pack' (@('pack', 'Bold.slnx') + $common)
    }
    else {
        $projects = @(Get-ChildItem -Path (Join-Path $root 'src') -Filter *.csproj -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
        $projects += Get-Item (Join-Path $root 'tools/Bold.CodeGen.Tool/Bold.CodeGen.Tool.csproj')
        foreach ($project in $projects) {
            if ((Get-Content $project.FullName -Raw) -match 'net10\.0-windows') {
                Write-Host "(not on this system: $($project.BaseName))"
                continue
            }
            Invoke-DotNet "pack-$($project.BaseName)" (@('pack', $project.FullName) + $common)
        }
    }
    $core = Get-ChildItem -Path $packages -Filter 'Bold.Core.*.nupkg' | Where-Object { $_.Name -notmatch '\.symbols\.' } | Select-Object -First 1
    if (-not $core) {
        throw "No packages were written to $packages"
    }
    $packed = $core.BaseName.Substring('Bold.Core.'.Length)
    $count = @(Get-ChildItem -Path $packages -Filter *.nupkg).Count
    Write-Host "$count packages, version $packed, in $packages"

    Step "The package source '$SourceName'"
    Clear-CachedPackages
    if (Test-Source $SourceName) {
        Invoke-DotNet 'source' @('nuget', 'update', 'source', $SourceName, '--source', $packages)
        Write-Host "Updated: $SourceName is $packages"
    }
    else {
        Invoke-DotNet 'source' @('nuget', 'add', 'source', $packages, '--name', $SourceName)
        Write-Host "Added: $SourceName is $packages"
    }

    Step 'bold-codegen'
    # An installed tool of this version would be kept as it is: it goes first, and comes back from the new package.
    Remove-Tool
    Invoke-DotNet 'tool' @('tool', 'install', '--global', 'bold-codegen', '--version', $packed, '--add-source', $packages)
    Write-Host "Installed bold-codegen $packed as a global tool (bold-codegen Model.bld --out <folder>)"

    $programs = @()
    if (-not $NoPrograms) {
        Step 'Programs'
        $wanted = @(
            @{ Name = 'propagator'; Project = 'src/Bold.Propagator.Server'; Program = 'Bold.Propagator.Server'; Windows = $false },
            @{ Name = 'remote'; Project = 'src/Bold.Remote.Server'; Program = 'Bold.Remote.Server'; Windows = $false },
            @{ Name = 'model-editor'; Project = 'src/Bold.ModelEditor'; Program = 'Bold.ModelEditor'; Windows = $true },
            @{ Name = 'propagator-analyzer'; Project = 'src/Bold.PropagatorAnalyzer'; Program = 'Bold.PropagatorAnalyzer'; Windows = $true }
        )
        foreach ($program in $wanted) {
            if ($program.Windows -and -not $onWindows) {
                Write-Host "(not on this system: $($program.Program))"
                continue
            }
            $folder = Join-Path $publish $program.Name
            Invoke-DotNet "publish-$($program.Name)" (@('publish', $program.Project, '-o', $folder) + $common)
            $file = if ($onWindows) { "$($program.Program).exe" } else { $program.Program }
            $path = Join-Path $folder $file
            if (-not (Test-Path $path)) {
                throw "$($program.Program) was not published to $folder"
            }
            $programs += $path
            Write-Host $path
        }
    }

    $editor = Join-Path (Join-Path $publish 'model-editor') 'Bold.ModelEditor.exe'
    if ($onWindows -and (Test-Path $editor)) {
        Step 'Model files (.bld)'
        Set-ModelFileType $editor
        Write-Host "A double click on a .bld file opens it in $editor"
    }

    $inVisualStudio = $false
    if ($onWindows -and -not $NoVisualStudio) {
        if (Test-VisualStudio) {
            if ($NoBuild) {
                & (Join-Path $root 'vs\build.ps1') -Install -NoBuild
            }
            else {
                & (Join-Path $root 'vs\build.ps1') -Install
            }
            $inVisualStudio = $true
        }
        else {
            Write-Host ''
            Write-Host '(Visual Studio was not found: its extension is not installed. Run CSharp\vs\build.ps1 -Install once it is there.)'
        }
    }

    Write-Host ''
    Write-Host "Bold for C# $packed is installed."
    Write-Host ''
    Write-Host 'In a project of your own:'
    Write-Host '  dotnet add package Bold.Persistence.Sql'
    Write-Host '  dotnet add package Bold.Handles'
    if ($onWindows) {
        Write-Host '  dotnet add package Bold.WinForms'
    }
    Write-Host '  dotnet add package Microsoft.Data.Sqlite'
    Write-Host ''
    Write-Host 'Then:'
    Write-Host '  bold-codegen Model.bld --out Generated      typed classes from a model'
    if ($programs.Count -gt 0 -and $onWindows) {
        Write-Host "  $(Join-Path (Join-Path $publish 'model-editor') 'Bold.ModelEditor.exe') Model.bld      the model editor"
    }
    Write-Host '  dotnet run --project examples/Library/Basics      the first tutorial (docs/TUTORIALS.md), from the CSharp folder'
    if ($inVisualStudio) {
        Write-Host ''
        Write-Host 'In Visual Studio: Tools > Bold has the model editor, class generation, the user guide and the tutorials;'
        Write-Host 'a .bld file opens in the model editor, and its right-click menu in Solution Explorer generates its classes.'
    }
    Write-Host ''
    Write-Host 'If bold-codegen is not found, open a new terminal: a first global tool adds its folder to the path.'
}
finally {
    Pop-Location
}
