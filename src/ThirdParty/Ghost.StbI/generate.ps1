param(
    [string]$Commit = "1da9ade1e66195252114f5da497f244704a6c58d",
    [string]$SourceDir = "",
    [string]$OutputDir = "$PSScriptRoot\Generated",
    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\..\Generate.Common.ps1"

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_stbi_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    if (-not $SourceDir) {
        $SourceDir = Get-GitHubArchive -Repo "nothings/stb" -Ref $Commit -TempDir $tempDir -IsCommit
    }

    if (-not $SkipNativeBuild) {
        $outDll = "$PSScriptRoot\runtimes\win-x64\native\stbi.dll"
        Build-CMakeNativeDll -SourceDir "$PSScriptRoot\native" -TempDir $tempDir -OutDllPath $outDll -BuiltDllName "stbi.dll" -CMakeArgs @("-DSTB_SOURCE_DIR=$SourceDir")
    }

    $stbiHeader = Join-Path $SourceDir "stb_image.h"
    $stbiWriteHeader = Join-Path $SourceDir "stb_image_write.h"
    if (-not (Test-Path $stbiHeader) -or -not (Test-Path $stbiWriteHeader)) {
        throw "stb_image.h or stb_image_write.h not found under '$SourceDir'."
    }

    $entryHeader = Join-Path $tempDir "stbi_entry.h"
    Set-Content -Path $entryHeader -Value @"
#include "$($stbiHeader -replace '\\', '/')"
#include "$($stbiWriteHeader -replace '\\', '/')"
"@ -Encoding UTF8

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
enum-operators=false
file-scoped-namespaces
helper-types
macro-bindings
--exclude
*_from_file*
--library-path
stbi
--namespace
Ghost.StbI
--method-class-name
Api
--file
$entryHeader
--traverse
$stbiHeader
--traverse
$stbiWriteHeader
--output
$OutputDir
"@

    Write-Host "Generating Ghost.StbI ($($Commit.Substring(0, 7))) bindings..."
    Invoke-ClangSharpGenerator -RspContent $rspContent -TempDir $tempDir -OutputDir $OutputDir

    Write-Host "Generating Ghost.StbI wrappers..."
    Invoke-GhostNativeWrapperGen -RepoRoot $repoRoot -ConfigPath "$repoRoot\src\Tools\Ghost.NativeWrapperGen\configs\stbi.json" -InputDir $OutputDir -OutputDir "$PSScriptRoot\Wrapper"
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
