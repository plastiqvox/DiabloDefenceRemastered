# 원작 DiabloDefence 실행 파일에 들어 있는 그림 자원을 꺼내 src/DiabloDefenceRemastered/Assets 에 PNG 로 둔다.
# 그림은 디아블로 1 자원이라 저장소에 올리지 않는다. 클론한 뒤 이 스크립트를 한 번 돌린다.
#
#   powershell -ExecutionPolicy Bypass -File tools\import-assets.ps1 -Source "G:\Workspace\DiabloDefence\bin\Debug\DiabloDefence.exe"
#
# Windows PowerShell 5.1(.NET Framework)에서 돌린다. 리소스의 Bitmap 을 되살리는 데 .NET Framework 가 필요하다.
param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [string]$Destination = (Join-Path $PSScriptRoot "..\src\DiabloDefenceRemastered\Assets")
)
$ErrorActionPreference = "Stop"

if ($PSVersionTable.PSEdition -eq "Core") {
    throw "Windows PowerShell 5.1 (powershell.exe) 로 실행해야 한다. pwsh 에서는 리소스 Bitmap 을 읽을 수 없다."
}
Add-Type -AssemblyName System.Drawing

$Source = (Resolve-Path $Source).Path
New-Item -ItemType Directory -Force $Destination | Out-Null
$Destination = (Resolve-Path $Destination).Path

$asm = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($Source))
$stream = $asm.GetManifestResourceStream("DiabloDefence.Properties.Resources.resources")
if ($stream -eq $null) { throw "원작 리소스가 없는 파일이다: $Source" }

$reader = New-Object System.Resources.ResourceReader($stream)
$count = 0
$e = $reader.GetEnumerator()
while ($e.MoveNext()) {
    $name = [string]$e.Key
    $value = $e.Value
    if ($value -is [System.Drawing.Bitmap]) {
        $value.Save((Join-Path $Destination "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $value.Dispose()
        $count++
    }
    elseif ($value -is [byte[]] -and $name -eq "DIInop") {
        [IO.File]::WriteAllBytes((Join-Path $Destination "DIInop.cur"), $value)
        $count++
    }
}
$reader.Close()
Write-Host "자원 $count 개를 $Destination 에 저장했다."
