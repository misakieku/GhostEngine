param(
    [string]$OutputDir = "$PSScriptRoot\Generated"
)

$ErrorActionPreference = "Stop"

# Resolve Microsoft.Direct3D.D3D12 package version from Ghost.D3D12.csproj
[xml]$csproj = Get-Content "$PSScriptRoot\Ghost.D3D12.csproj"
$d3d12Pkg = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq "Microsoft.Direct3D.D3D12" } | Select-Object -First 1
if (-not $d3d12Pkg) {
    throw "Microsoft.Direct3D.D3D12 PackageReference not found in Ghost.D3D12.csproj"
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

# Clean previous generated output
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

$rspPath = Join-Path ([System.IO.Path]::GetTempPath()) ("ghost_d3d12_" + [Guid]::NewGuid().ToString("N") + ".rsp")
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
marker-interfaces
vtbl-index-attribute
--define-macro
INITGUID
--library-path
d3d12
--namespace
Ghost.D3D12
--method-class-name
D3D12
--remap
tagRECT=RECT
_LUID=LUID
_SECURITY_ATTRIBUTES=SECURITY_ATTRIBUTES
HWND__*=HWND
--include-directory
$d3d12Include
--include-directory
$sdkInclude\um
--include-directory
$sdkInclude\shared
--include-directory
$sdkInclude\winrt
--file
$d3d12Include\d3d12.h
--traverse
$d3d12Include\d3d12.h
--traverse
$d3d12Include\d3d12sdklayers.h
--traverse
$d3d12Include\d3dcommon.h
--output
$OutputDir
"@

try {
    Set-Content -Path $rspPath -Value $rspContent -Encoding UTF8

    Write-Host "Generating D3D12 ($d3d12Version) bindings using Windows SDK $sdkVersion..."
    ClangSharpPInvokeGenerator "@$rspPath"
    Write-Host "Generation finished (exit code: $LASTEXITCODE)."

    # Work around ClangSharp bug where callback pointer parameters inside MemberFunction vtbl casts lose their [Cdecl] calling convention
    $dumpFilePath = Join-Path $OutputDir "ID3D12DeviceDumpFile.cs"
    if (Test-Path $dumpFilePath) {
        $dumpContent = Get-Content $dumpFilePath -Raw
        $dumpContent = $dumpContent.Replace("delegate* unmanaged[MemberFunction]<ulong, uint>", "delegate* unmanaged[Cdecl]<ulong, uint>")
        $dumpContent = $dumpContent.Replace("delegate* unmanaged[MemberFunction]<char*, void>", "delegate* unmanaged[Cdecl]<char*, void>")
        Set-Content -Path $dumpFilePath -Value $dumpContent -Encoding UTF8 -NoNewline
    }
}
finally {
    Remove-Item $rspPath -Force -ErrorAction SilentlyContinue
}
