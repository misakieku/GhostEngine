param(
    [string]$Tag = "v0.23.0",
    [string]$SourceDir = "",
    [string]$OutputDir = "$PSScriptRoot\Generated",
    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\..\Generate.Common.ps1"

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_ufbx_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    if (-not $SourceDir) {
        $SourceDir = Get-GitHubArchive -Repo "ufbx/ufbx" -Ref $Tag -TempDir $tempDir
    }

    if (-not $SkipNativeBuild) {
        $outDll = "$PSScriptRoot\runtimes\win-x64\native\ufbx.dll"
        Build-CMakeNativeDll -SourceDir "$PSScriptRoot\native" -TempDir $tempDir -OutDllPath $outDll -BuiltDllName "ufbx.dll" -CMakeArgs @("-DUFBX_SOURCE_DIR=$SourceDir")
    }

    $header = Join-Path $SourceDir "ufbx.h"
    if (-not (Test-Path $header)) {
        throw "ufbx.h not found at '$header'."
    }

    $rspContent = @"
--language
c
--std
c11
--config
codegen=latest
file=multi
--generate
aggressive-inlining
disable-runtime-marshalling
doc-includes
enum-operators=false
file-scoped-namespaces
funcs-with-body=false
helper-types
macro-bindings
--with-constant-folded-value
UFBX_*_COUNT
--define-macro
UFBX_REAL_IS_FLOAT
--library-path
ufbx
--namespace
Ghost.Ufbx
--method-class-name
Api
--file
$header
--traverse
$header
--output
$OutputDir
"@

    Write-Host "Generating Ghost.Ufbx ($Tag) bindings..."
    Invoke-ClangSharpGenerator -RspContent $rspContent -TempDir $tempDir -OutputDir $OutputDir

    Write-Host "Generating Ghost.Ufbx wrappers..."
    Invoke-GhostNativeWrapperGen -RepoRoot $repoRoot -ConfigPath "$repoRoot\src\Tools\Ghost.NativeWrapperGen\configs\ufbx.json" -InputDir $OutputDir -OutputDir "$PSScriptRoot\Wrapper"
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
