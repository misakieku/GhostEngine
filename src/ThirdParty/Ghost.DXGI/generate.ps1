param(
    [string]$OutputDir = "$PSScriptRoot\Generated"
)

$ErrorActionPreference = "Stop"

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

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_dxgi_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    $entryHeader = Join-Path $tempDir "dxgi_entry.h"
    Set-Content -Path $entryHeader -Value @"
#pragma once
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <dxgi1_6.h>
#include <dxgidebug.h>
"@ -Encoding UTF8

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
vtbl-index-attribute
--define-macro
INITGUID
--library-path
dxgi
--with-library-path
DXGIGetDebugInterface=dxgidebug
--namespace
Ghost.DXGI
--method-class-name
DXGI
--remap
tagRECT=RECT
tagPOINT=POINT
_LUID=LUID
_LARGE_INTEGER=LARGE_INTEGER
_SECURITY_ATTRIBUTES=SECURITY_ATTRIBUTES
HWND__*=HWND
HMONITOR__*=HMONITOR
HDC__*=HDC
HINSTANCE__*=HMODULE
--with-transparent-struct
HRESULT=int
--include-directory
$sdkInclude\shared
--include-directory
$sdkInclude\um
--include-directory
$sdkInclude\winrt
--file
$entryHeader
--traverse
$sdkInclude\shared\dxgi.h
--traverse
$sdkInclude\shared\dxgi1_2.h
--traverse
$sdkInclude\shared\dxgi1_3.h
--traverse
$sdkInclude\shared\dxgi1_4.h
--traverse
$sdkInclude\shared\dxgi1_5.h
--traverse
$sdkInclude\shared\dxgi1_6.h
--traverse
$sdkInclude\shared\dxgicommon.h
--traverse
$sdkInclude\shared\dxgiformat.h
--traverse
$sdkInclude\shared\dxgitype.h
--traverse
$sdkInclude\um\dxgidebug.h
--output
$OutputDir
"@ -Encoding UTF8

    Write-Host "Generating DXGI bindings using Windows SDK $sdkVersion..."
    ClangSharpPInvokeGenerator "@$rspPath"
    Write-Host "Generation finished (exit code: $LASTEXITCODE)."
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
