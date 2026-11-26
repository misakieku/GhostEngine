param(
    [string]$OutputDir = "$PSScriptRoot\Generated"
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\..\Generate.Common.ps1"

$dxcVersion = Resolve-PackageVersionFromCsproj -CsprojPath "$PSScriptRoot\Ghost.DXC.csproj" -PackageId "Microsoft.Direct3D.DXC"
$nugetCache = Resolve-NuGetGlobalPackagesPath

$dxcInclude = Join-Path $nugetCache "microsoft.direct3d.dxc\$dxcVersion\build\native\include"
if (-not (Test-Path $dxcInclude)) {
    throw "DXC include directory not found at '$dxcInclude'. Run 'dotnet restore' first."
}

$winSdk = Resolve-WindowsSdkIncludeDir
$sdkInclude = $winSdk.Path

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_dxc_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    $entryHeader = Join-Path $tempDir "dxc_entry.h"
    Set-Content -Path $entryHeader -Value @"
#pragma once
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <Unknwn.h>
#include <objidl.h>
#include <dxcapi.h>
#include <dxcerrors.h>
#include <dxcisense.h>
"@ -Encoding UTF8

    $rspContent = @"
--language
c++
--std
c++17
--config
codegen=latest
file=multi
types=windows
vtbls=implicit
--generate
aggressive-inlining
callconv-member-function
disable-runtime-marshalling
enum-operators=false
file-scoped-namespaces
guid-member
macro-bindings
marker-interfaces
native-inheritance-attribute
unmanaged-constants
vtbl-index-attribute
--define-macro
INITGUID
--exclude
IUnknown
IMalloc
IStream
STATSTG
FILETIME
LARGE_INTEGER
--library-path
dxcompiler
--namespace
Ghost.DXC
--method-class-name
Api
--with-transparent-struct
HRESULT=int
--include-directory
$dxcInclude
--include-directory
$sdkInclude\um
--include-directory
$sdkInclude\shared
--include-directory
$sdkInclude\winrt
--file
$entryHeader
--traverse
$dxcInclude\dxcapi.h
--traverse
$dxcInclude\dxcerrors.h
--traverse
$dxcInclude\dxcisense.h
--output
$OutputDir
"@

    Write-Host "Generating DXC ($dxcVersion) bindings using Windows SDK $($winSdk.Version)..."
    Invoke-ClangSharpGenerator -RspContent $rspContent -TempDir $tempDir -OutputDir $OutputDir
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
