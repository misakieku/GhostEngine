param(
    [string]$Tag = "v1.3",
    [string]$SourceDir = "",
    [string]$OutputDir = "$PSScriptRoot\Generated",
    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\..\Generate.Common.ps1"

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_meshopt_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    if (-not $SourceDir) {
        $SourceDir = Get-GitHubArchive -Repo "zeux/meshoptimizer" -Ref $Tag -TempDir $tempDir
    }

    if (-not $SkipNativeBuild) {
        $outDll = "$PSScriptRoot\runtimes\win-x64\native\meshoptimizer.dll"
        Build-CMakeNativeDll -SourceDir $SourceDir -TempDir $tempDir -OutDllPath $outDll -BuiltDllName "meshoptimizer.dll" -CMakeArgs @("-DMESHOPT_BUILD_SHARED_LIBS=ON")
    }

    $header = Join-Path $SourceDir "src\meshoptimizer.h"
    if (-not (Test-Path $header)) {
        throw "meshoptimizer.h not found at '$header'."
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
helper-types
macro-bindings
--library-path
meshoptimizer
--namespace
Ghost.MeshOptimizer
--method-class-name
Api
--file
$header
--traverse
$header
--output
$OutputDir
"@

    Write-Host "Generating Ghost.MeshOptimizer ($Tag) bindings..."
    Invoke-ClangSharpGenerator -RspContent $rspContent -TempDir $tempDir -OutputDir $OutputDir

    Write-Host "Generating Ghost.MeshOptimizer wrappers..."
    Invoke-GhostNativeWrapperGen -RepoRoot $repoRoot -ConfigPath "$repoRoot\src\Tools\Ghost.NativeWrapperGen\configs\meshopt.json" -InputDir $OutputDir -OutputDir "$PSScriptRoot\Wrapper"
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
