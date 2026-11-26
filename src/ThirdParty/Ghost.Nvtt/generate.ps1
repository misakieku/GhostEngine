param(
    [string]$SdkDir = $(if ($env:NVTT_SDK_DIR) { $env:NVTT_SDK_DIR } else { "${env:ProgramFiles}\NVIDIA Corporation\NVIDIA Texture Tools" }),
    [string]$OutputDir = "$PSScriptRoot\Generated",
    [switch]$SkipNativeCopy
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\..\Generate.Common.ps1"

if (-not (Test-Path $SdkDir)) {
    throw "NVTT SDK directory not found at '$SdkDir'. Install NVIDIA Texture Tools or pass -SdkDir / set NVTT_SDK_DIR."
}

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_nvtt_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    if (-not $SkipNativeCopy) {
        $sdkDll = Get-ChildItem -Path $SdkDir -Filter "nvtt*.dll" -File |
            Where-Object { $_.Name -match '^nvtt\d*\.dll$' } |
            Sort-Object Name -Descending |
            Select-Object -First 1
        if (-not $sdkDll) {
            throw "Could not find nvtt*.dll in '$SdkDir'."
        }

        $nativeOut = "$PSScriptRoot\runtimes\win-x64\native"
        New-Item -ItemType Directory -Path $nativeOut -Force | Out-Null
        Copy-Item $sdkDll.FullName "$nativeOut\nvtt.dll" -Force
        Write-Host "Copied $($sdkDll.Name) -> runtimes\win-x64\native\nvtt.dll"
    }

    $header = Join-Path $SdkDir "include\nvtt\nvtt_wrapper.h"
    if (-not (Test-Path $header)) {
        throw "nvtt_wrapper.h not found at '$header'."
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
enum-operators=false
file-scoped-namespaces
helper-types
macro-bindings
--library-path
nvtt
--exclude
NvttBoolean
--namespace
Ghost.Nvtt
--method-class-name
Api
--include-directory
$SdkDir\include
--file
$header
--traverse
$header
--output
$OutputDir
"@

    Write-Host "Generating Ghost.Nvtt bindings from '$SdkDir'..."
    Invoke-ClangSharpGenerator -RspContent $rspContent -TempDir $tempDir -OutputDir $OutputDir

    Write-Host "Generating Ghost.Nvtt wrappers..."
    Invoke-GhostNativeWrapperGen -RepoRoot $repoRoot -ConfigPath "$repoRoot\src\Tools\Ghost.NativeWrapperGen\configs\nvtt.json" -InputDir $OutputDir -OutputDir "$PSScriptRoot\Wrapper"
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
