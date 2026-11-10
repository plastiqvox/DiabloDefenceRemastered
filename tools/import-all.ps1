# 두 원본을 한 명령으로 가져온다 (설계서 1.4 "실행", 소유 WP0, 기획 19.2): 원작 exe PNG(import-assets.ps1) → diabdat(DiabloAssetImporter).
# 결과는 src/DiabloDefenceRemastered/Assets 에 쓰고 저장소에는 올리지 않는다(.gitignore). 클론한 뒤 한 번 돌린다.
#
#   powershell -ExecutionPolicy Bypass -File tools\import-all.ps1 -Diabdat "<…>\diabdat.mpq" -Exe "<…>\DiabloDefence.exe"
#       [-Hellfire "<폴더>"] [-SkipBundles "<묶음,…|all>"] [-Upscale realesrgan-x4plus] [-UpscaleBin "<폴더>"] [-UpscaleGpu <n>]
#
# 도구 옵션은 그대로 넘긴다. 도구의 종료 코드 0 과 6(AI 확대를 못 해 원본 픽셀로 만듦 — Assets 는 온전함)을 성공으로 본다.
# import-assets.ps1 이 Windows PowerShell 5.1 을 요구하므로 이 스크립트도 powershell.exe 로 돌린다.
param(
    [Parameter(Mandatory = $true)]
    [string]$Diabdat,
    [Parameter(Mandatory = $true)]
    [string]$Exe,
    [string]$Hellfire,
    [string]$SkipBundles,
    [string]$Upscale,
    [string]$UpscaleBin,
    [string]$UpscaleGpu
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8   # 도구는 UTF-8 로 쓴다 — 받는 쪽도 맞춰야 경고 줄을 고를 수 있다

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root "src\DiabloDefenceRemastered\Assets"

# 1. 원작 exe PNG — Assets 뿌리(d1/ 밖)
& (Join-Path $PSScriptRoot "import-assets.ps1") -Source $Exe -Destination $assets
if (-not $?) { exit 1 }

# 2. diabdat — Assets\d1. 저장소 뿌리에서 돌려 기본 --out 이 이 저장소의 Assets 가 되게 한다
$toolArgs = @("run", "--project", (Join-Path $root "tools\DiabloAssetImporter"), "-c", "Release", "--", "import", "--diabdat", $Diabdat)
if ($PSBoundParameters.ContainsKey("Hellfire")) { $toolArgs += @("--hellfire", $Hellfire) }
if ($SkipBundles) { $toolArgs += @("--skip-bundles", $SkipBundles) }
if ($Upscale) { $toolArgs += @("--upscale", $Upscale) }
if ($UpscaleBin) { $toolArgs += @("--upscale-bin", $UpscaleBin) }
if ($UpscaleGpu) { $toolArgs += @("--upscale-gpu", $UpscaleGpu) }

Push-Location $root
try {
    $lines = New-Object System.Collections.Generic.List[string]
    & dotnet @toolArgs | ForEach-Object { Write-Host $_; $lines.Add([string]$_) }
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
}

switch ($code) {
    0 { }
    6 {
        # 끝 보고 맨 끝의 경고 줄을 다시 보인다(1.4 "되돌아감")
        $lines | Where-Object { $_ -like "경고: AI 확대*" } | ForEach-Object { Write-Warning $_ }
        $code = 0
    }
    default {
        if ($code -eq 2 -or $code -eq 3) {
            if ($PSBoundParameters.ContainsKey("Hellfire")) {
                Write-Host "헬파이어 없이 가져오려면: -SkipBundles hellfire (또는 all)"
            }
        }
        Write-Error "가져오기 실패(종료 코드 $code)" -ErrorAction Continue
    }
}
exit $code
