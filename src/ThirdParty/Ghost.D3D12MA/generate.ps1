param(
    [string]$OutputDir = "$PSScriptRoot\Generated",
    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

# Resolve Microsoft.Direct3D.D3D12 package version from Ghost.D3D12.csproj
$d3d12CsprojPath = Resolve-Path "$PSScriptRoot\..\Ghost.D3D12\Ghost.D3D12.csproj"
[xml]$csproj = Get-Content $d3d12CsprojPath
$d3d12Pkg = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq "Microsoft.Direct3D.D3D12" } | Select-Object -First 1
if (-not $d3d12Pkg) {
    throw "Microsoft.Direct3D.D3D12 PackageReference not found in $d3d12CsprojPath"
}
$d3d12Version = $d3d12Pkg.Version

# Resolve NuGet global-packages path
$nugetOut = dotnet nuget locals global-packages --list
if ($nugetOut -match "global-packages:\s*(.+)") {
    $nugetCache = $Matches[1].Trim()
} else {
    throw "Failed to resolve NuGet global-packages path: $nugetOut"
}

$d3d12Include = Join-Path $nugetCache "microsoft.direct3d.d3d12\$d3d12Version\build\native\include"
if (-not (Test-Path $d3d12Include)) {
    throw "D3D12 include directory not found at '$d3d12Include'. Run 'dotnet restore' first."
}

# Resolve latest installed Windows SDK include directory
$kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\Include"
$sdkVersion = Get-ChildItem $kitsRoot -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1 -ExpandProperty Name
if (-not $sdkVersion) {
    throw "No Windows 10/11 SDK found under '$kitsRoot'."
}
$sdkInclude = Join-Path $kitsRoot $sdkVersion

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_d3d12ma_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    if (-not $SkipNativeBuild) {
        $cmakeBuildDir = Join-Path $tempDir "cmake_build"
        Write-Host "Building d3d12ma.dll (Release|x64) against D3D12 $d3d12Version..."
        cmake -S "$PSScriptRoot\native" -B $cmakeBuildDir -A x64 "-DD3D12_INCLUDE_DIR=$d3d12Include"
        if ($LASTEXITCODE -ne 0) {
            throw "CMake configure failed."
        }

        cmake --build $cmakeBuildDir --config Release --parallel
        if ($LASTEXITCODE -ne 0) {
            throw "CMake build failed."
        }

        $nativeOutDir = "$PSScriptRoot\runtimes\win-x64\native"
        New-Item -ItemType Directory -Path $nativeOutDir -Force | Out-Null
        Copy-Item "$cmakeBuildDir\Release\d3d12ma.dll" "$nativeOutDir\d3d12ma.dll" -Force
    }

    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }

    $wrapperHeader = "$PSScriptRoot\native\d3d12ma_c.h"
    $rspPath = Join-Path $tempDir "generate.rsp"
    Set-Content -Path $rspPath -Value @"
--language
c++
--std
c++17
--config
codegen=latest
file=multi
types=windows
--generate
aggressive-inlining
disable-runtime-marshalling
enum-operators=false
file-scoped-namespaces
--library-path
d3d12ma
--namespace
Ghost.D3D12MA
--method-class-name
Api
--with-transparent-struct
HRESULT=int
--include-directory
$d3d12Include
--include-directory
$sdkInclude\um
--include-directory
$sdkInclude\shared
--include-directory
$sdkInclude\winrt
--file
$wrapperHeader
--traverse
$wrapperHeader
--output
$OutputDir
"@ -Encoding UTF8

    Write-Host "Generating Ghost.D3D12MA bindings..."
    ClangSharpPInvokeGenerator "@$rspPath"
    Write-Host "Generation finished (exit code: $LASTEXITCODE)."

    Write-Host "Generating Ghost.D3D12MA struct wrappers..."
    dotnet run --project "$PSScriptRoot\..\..\Tools\Ghost.NativeWrapperGen\Ghost.NativeWrapperGen.csproj" -- --config "$PSScriptRoot\..\..\Tools\Ghost.NativeWrapperGen\configs\d3d12ma.json" --input "$OutputDir" --output "$PSScriptRoot\Wrapper"
    if ($LASTEXITCODE -ne 0) {
        throw "Ghost.NativeWrapperGen failed."
    }
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
