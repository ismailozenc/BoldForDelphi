<#
.SYNOPSIS
Builds Bold for Visual Studio (the VSIX), and installs it into Visual Studio when asked.

.DESCRIPTION
Publishes Bold's programs (the model editor, the propagator analyzer and bold-codegen) and the user guide into
CSharp/artifacts/vsix/tools, builds the extension with Visual Studio's MSBuild, and leaves it as
CSharp/artifacts/vsix/Bold.VisualStudio.vsix, with the programs inside. A double click on that file installs it too.

It needs Windows, the .NET 10 SDK and Visual Studio 2022 or later (Community, Professional or Enterprise). The extension
puts a Bold submenu in the Tools menu, opens .bld files in the model editor, and generates a model's classes from
Solution Explorer. The command line tools stay as they are.

.PARAMETER Install
Installs the extension into every edition of Visual Studio it fits, after building it. Close Visual Studio first.

.PARAMETER Uninstall
Removes the extension from Visual Studio; nothing is built.

.PARAMETER NoBuild
Publishes what "dotnet build CSharp/Bold.slnx -c Release" already built.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File CSharp\vs\build.ps1 -Install
#>
[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$Uninstall,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path (Join-Path $root 'artifacts') 'vsix'
$tools = Join-Path $out 'tools'
$logs = Join-Path $out 'logs'
$vsix = Join-Path $out 'Bold.VisualStudio.vsix'
# The Identity Id of source.extension.vsixmanifest.
$extensionId = 'Bold.VisualStudio.6b1e6f4c-2c7a-4c0b-9e8a-5b0c1d2e3f41'

if (($PSVersionTable.PSVersion.Major -ge 6) -and -not $IsWindows) {
    throw 'The Visual Studio extension is built on Windows: Visual Studio runs there only.'
}

function Step([string]$text) {
    Write-Host ''
    Write-Host "== $text"
}

# Runs a program quietly, its output in a log; shows the log and stops when it fails.
function Invoke-Quiet([string]$name, [string]$program, [string[]]$arguments) {
    $log = Join-Path $logs "$name.log"
    $preference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $program @arguments > $log 2>&1
    }
    finally {
        $ErrorActionPreference = $preference
    }
    if ($LASTEXITCODE -ne 0) {
        Get-Content $log | Write-Host
        throw "$([System.IO.Path]::GetFileName($program)) failed (exit code $LASTEXITCODE); its output is in $log"
    }
}

# Visual Studio's folder: the newest one installed, previews too.
function Find-VisualStudio {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        return $null
    }
    $found = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($found) {
        return @($found)[0]
    }
    return $null
}

# Runs VSIXInstaller and waits for it; returns its exit code.
function Invoke-VsixInstaller([string]$visualStudio, [string[]]$arguments) {
    $installer = Join-Path $visualStudio 'Common7\IDE\VSIXInstaller.exe'
    if (-not (Test-Path $installer)) {
        throw "VSIXInstaller.exe is not in $visualStudio"
    }
    $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru
    return $process.ExitCode
}

$visualStudio = Find-VisualStudio
if (-not $visualStudio) {
    throw 'Visual Studio was not found (2022 or later, with MSBuild). Install Visual Studio Community from https://visualstudio.microsoft.com and run this again.'
}
Write-Host "Visual Studio: $visualStudio"

if ($Uninstall) {
    Step 'Removing Bold for Visual Studio'
    $code = Invoke-VsixInstaller $visualStudio @('/quiet', "/uninstall:$extensionId")
    if ($code -eq 0) {
        Write-Host 'Removed.'
    }
    else {
        Write-Host "VSIXInstaller answered ${code}: the extension was not installed, or Visual Studio is open (close it and run this again)."
    }
    return
}

Push-Location $root
try {
    if (Test-Path $tools) {
        Remove-Item -Recurse -Force $tools
    }
    New-Item -ItemType Directory -Force -Path $tools, $logs | Out-Null
    $common = @('-c', 'Release', '--nologo')
    if ($NoBuild) {
        $common += '--no-build'
    }

    Step "Bold's programs, for the extension"
    $programs = @(
        @{ Name = 'model-editor'; Project = 'src/Bold.ModelEditor'; File = 'Bold.ModelEditor.exe' },
        @{ Name = 'propagator-analyzer'; Project = 'src/Bold.PropagatorAnalyzer'; File = 'Bold.PropagatorAnalyzer.exe' },
        @{ Name = 'codegen'; Project = 'tools/Bold.CodeGen.Tool'; File = 'bold-codegen.dll' }
    )
    foreach ($program in $programs) {
        $folder = Join-Path $tools $program.Name
        Invoke-Quiet "publish-$($program.Name)" 'dotnet' (@('publish', $program.Project, '-o', $folder) + $common)
        if (-not (Test-Path (Join-Path $folder $program.File))) {
            throw "$($program.File) was not published to $folder"
        }
        # The debug symbols are not needed in Visual Studio's extension folder.
        Get-ChildItem -Path $folder -Filter *.pdb -Recurse | Remove-Item -Force
        Write-Host (Join-Path $folder $program.File)
    }
    $docs = Join-Path $tools 'docs'
    New-Item -ItemType Directory -Force -Path $docs | Out-Null
    Copy-Item (Join-Path $root 'docs\USER_GUIDE.md'), (Join-Path $root 'docs\TUTORIALS.md') $docs

    Step 'The extension'
    $msbuild = Join-Path $visualStudio 'MSBuild\Current\Bin\MSBuild.exe'
    if (-not (Test-Path $msbuild)) {
        throw "MSBuild.exe is not in $visualStudio"
    }
    $project = Join-Path $root 'vs\Bold.VisualStudio\Bold.VisualStudio.csproj'
    Invoke-Quiet 'vsix' $msbuild @($project, '-restore', '-nologo', '-v:minimal', '-p:Configuration=Release', "-p:BoldToolsDir=$tools\")
    $built = Join-Path $root 'vs\Bold.VisualStudio\bin\Release\Bold.VisualStudio.vsix'
    if (-not (Test-Path $built)) {
        Get-Content (Join-Path $logs 'vsix.log') | Write-Host
        throw "The build did not write $built"
    }
    Copy-Item $built $vsix -Force
    Write-Host "$vsix ($([int]((Get-Item $vsix).Length / 1024)) KB)"

    if ($Install) {
        Step 'Installing into Visual Studio'
        # An installed extension of this version would be kept as it is: it goes first.
        Invoke-VsixInstaller $visualStudio @('/quiet', "/uninstall:$extensionId") | Out-Null
        $code = Invoke-VsixInstaller $visualStudio @('/quiet', $vsix)
        if ($code -ne 0) {
            throw "VSIXInstaller answered $code. Close every Visual Studio window and run this again, or double-click $vsix to see what it says."
        }
        Write-Host 'Installed. In Visual Studio: Tools > Bold; a double click on a .bld file opens the model editor;'
        Write-Host 'a right click on a .bld file in Solution Explorer has "Generate Bold Classes".'
    }
    else {
        Write-Host ''
        Write-Host "Install it with -Install, or with a double click on $vsix."
    }
}
finally {
    Pop-Location
}
