Set-StrictMode -Version Latest

function Resolve-NuGetGlobalPackagesPath {
    $nugetOut = dotnet nuget locals global-packages --list
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet nuget locals failed."
    }
    if ($nugetOut -match "global-packages:\s*(.+)") {
        return $Matches[1].Trim()
    }
    throw "Failed to resolve NuGet global-packages path: $nugetOut"
}

function Resolve-PackageVersionFromCsproj {
    param(
        [Parameter(Mandatory = $true)][string]$CsprojPath,
        [Parameter(Mandatory = $true)][string]$PackageId
    )

    [xml]$csproj = Get-Content $CsprojPath
    $pkg = $csproj.SelectNodes("//PackageReference") | Where-Object { $_.Include -eq $PackageId } | Select-Object -First 1
    if (-not $pkg) {
        throw "$PackageId PackageReference not found in $CsprojPath"
    }
    return $pkg.Version
}

function Resolve-WindowsSdkIncludeDir {
    $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\Include"
    if (-not (Test-Path $kitsRoot)) {
        throw "Windows Kits Include directory not found at '$kitsRoot'."
    }
    $sdkVersion = Get-ChildItem $kitsRoot -Directory |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
        Sort-Object { [version]$_.Name } -Descending |
        Select-Object -First 1 -ExpandProperty Name
    if (-not $sdkVersion) {
        throw "No Windows 10/11 SDK found under '$kitsRoot'."
    }
    return @{
        Version = $sdkVersion
        Path = (Join-Path $kitsRoot $sdkVersion)
    }
}

function Invoke-ClangSharpGenerator {
    param(
        [Parameter(Mandatory = $true)][string]$RspContent,
        [Parameter(Mandatory = $true)][string]$TempDir,
        [Parameter(Mandatory = $true)][string]$OutputDir
    )

    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }

    $rspPath = Join-Path $TempDir "clangsharp.rsp"
    Set-Content -Path $rspPath -Value $RspContent -Encoding UTF8

    $clangOutput = & ClangSharpPInvokeGenerator "@$rspPath" 2>&1
    $clangExit = $LASTEXITCODE
    $clangOutput | ForEach-Object { Write-Host $_ }

    if ($clangExit -lt 0) {
        throw "ClangSharpPInvokeGenerator failed with exit code $clangExit."
    }
    if ($clangOutput -match '^\s*Error\s*\(') {
        throw "ClangSharpPInvokeGenerator reported errors."
    }
    if (-not (Test-Path $OutputDir) -or @(Get-ChildItem $OutputDir -Filter *.cs).Count -eq 0) {
        throw "ClangSharpPInvokeGenerator produced no output files in '$OutputDir'."
    }
}

function Invoke-GhostNativeWrapperGen {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$ConfigPath,
        [Parameter(Mandatory = $true)][string]$InputDir,
        [Parameter(Mandatory = $true)][string]$OutputDir
    )

    $toolCsproj = Join-Path $RepoRoot "src\Tools\Ghost.NativeWrapperGen\Ghost.NativeWrapperGen.csproj"
    dotnet run --project $toolCsproj -- --config $ConfigPath --input $InputDir --output $OutputDir
    if ($LASTEXITCODE -ne 0) {
        throw "Ghost.NativeWrapperGen failed with exit code $LASTEXITCODE."
    }
}

function Get-GitHubArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Repo,
        [Parameter(Mandatory = $true)][string]$Ref,
        [Parameter(Mandatory = $true)][string]$TempDir,
        [switch]$IsCommit
    )

    $archiveUrl = if ($IsCommit) {
        "https://github.com/$Repo/archive/$Ref.zip"
    } else {
        "https://github.com/$Repo/archive/refs/tags/$Ref.zip"
    }

    $zipPath = Join-Path $TempDir "source.zip"
    $extractDir = Join-Path $TempDir "src"

    Write-Host "Downloading $archiveUrl..."
    Invoke-WebRequest -Uri $archiveUrl -OutFile $zipPath -UseBasicParsing
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

    $rootDir = Get-ChildItem $extractDir -Directory | Select-Object -First 1
    if (-not $rootDir) {
        throw "Failed to locate extracted source folder in $extractDir."
    }
    return $rootDir.FullName
}

function Build-CMakeNativeDll {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$TempDir,
        [Parameter(Mandatory = $true)][string]$OutDllPath,
        [Parameter(Mandatory = $true)][string]$BuiltDllName,
        [string[]]$CMakeArgs = @()
    )

    $buildDir = Join-Path $TempDir "cmake_build"
    $configureArgs = @("-S", $SourceDir, "-B", $buildDir, "-A", "x64") + $CMakeArgs
    Write-Host "Configuring CMake in $SourceDir..."
    & cmake @configureArgs
    if ($LASTEXITCODE -ne 0) {
        throw "CMake configure failed."
    }

    Write-Host "Building $BuiltDllName (Release|x64)..."
    cmake --build $buildDir --config Release --parallel
    if ($LASTEXITCODE -ne 0) {
        throw "CMake build failed."
    }

    $builtDll = Get-ChildItem -Path $buildDir -Filter $BuiltDllName -Recurse -File |
        Where-Object { $_.FullName -match '\\Release\\' } |
        Select-Object -First 1
    if (-not $builtDll) {
        throw "Could not find built '$BuiltDllName' under '$buildDir'."
    }

    $outDir = Split-Path -Parent $OutDllPath
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    Copy-Item $builtDll.FullName $OutDllPath -Force
}
