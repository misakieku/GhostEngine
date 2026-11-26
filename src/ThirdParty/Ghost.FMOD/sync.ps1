param(
    [string]$SdkDir = $(if ($env:FMOD_SDK_DIR) { $env:FMOD_SDK_DIR } else { "${env:ProgramFiles(x86)}\FMOD SoundSystem\FMOD Studio API Windows" }),
    [switch]$SkipNativeCopy
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SdkDir)) {
    throw "FMOD SDK directory not found at '$SdkDir'. Install FMOD Studio API Windows or pass -SdkDir / set FMOD_SDK_DIR."
}

$coreInc = Join-Path $SdkDir "api\core\inc"
$studioInc = Join-Path $SdkDir "api\studio\inc"

if (-not $SkipNativeCopy) {
    $nativeOut = "$PSScriptRoot\runtimes\win-x64\native"
    New-Item -ItemType Directory -Path $nativeOut -Force | Out-Null

    $fmodDll = Join-Path $SdkDir "api\core\lib\x64\fmod.dll"
    $studioDll = Join-Path $SdkDir "api\studio\lib\x64\fmodstudio.dll"
    if (-not (Test-Path $fmodDll) -or -not (Test-Path $studioDll)) {
        throw "FMOD x64 DLLs not found under '$SdkDir'."
    }

    Copy-Item $fmodDll "$nativeOut\fmod.dll" -Force
    Copy-Item $studioDll "$nativeOut\fmodstudio.dll" -Force
}

function Convert-FmodWrapper {
    param(
        [string]$SourcePath,
        [string]$DestPath,
        [switch]$IsStudio
    )

    $content = Get-Content $SourcePath -Raw

    if ($IsStudio) {
        $content = $content -replace 'namespace FMOD\.Studio', "using Ghost.FMOD.Core;`r`n`r`nnamespace Ghost.FMOD.Studio"
        $content = $content -replace '\bFMOD\.', ''
        $content = $content -replace 'public const string\s+dll\s*=\s*"fmodstudio";', 'public const string DLL = "fmodstudio";'
        $content = $content -replace 'STUDIO_VERSION\.dll', 'STUDIO_VERSION.DLL'
        $content = $content -replace 'VERSION\.number', 'VERSION.NUMBER'
    } else {
        $content = $content -replace 'namespace FMOD', 'namespace Ghost.FMOD.Core'
        $content = $content -replace 'public const int\s+number\s*=\s*([^;]+);', 'public const int NUMBER = $1;'
        $content = $content -replace 'public const string\s+dll\s*=\s*"fmod";', 'public const string DLL = "fmod";'
        $content = $content -replace 'VERSION\.dll', 'VERSION.DLL'
        $content = $content -replace 'VERSION\.number', 'VERSION.NUMBER'
    }

    $content = $content -replace '#if !UNITY_2021_3_OR_NEWER\r?\n', ''
    $content = $content -replace '#endif\r?\n', ''

    $destDir = Split-Path -Parent $DestPath
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    Set-Content -Path $DestPath -Value $content -Encoding UTF8 -NoNewline
}

Convert-FmodWrapper -SourcePath (Join-Path $coreInc "fmod.cs") -DestPath "$PSScriptRoot\Core\fmod.cs"
Convert-FmodWrapper -SourcePath (Join-Path $coreInc "fmod_dsp.cs") -DestPath "$PSScriptRoot\Core\fmod_dsp.cs"
Convert-FmodWrapper -SourcePath (Join-Path $coreInc "fmod_errors.cs") -DestPath "$PSScriptRoot\Core\fmod_errors.cs"
Convert-FmodWrapper -SourcePath (Join-Path $studioInc "fmod_studio.cs") -DestPath "$PSScriptRoot\Studio\fmod_studio.cs" -IsStudio

Write-Host "Synced Ghost.FMOD from '$SdkDir'."
