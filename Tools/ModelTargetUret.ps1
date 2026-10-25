# G6 - Vuforia Model Target üretimi (Model Target CLI)
# Kullanım: Bu dosyaya sağ tıklayıp "PowerShell ile çalıştır" veya:
#   powershell -ExecutionPolicy Bypass -File Tools\ModelTargetUret.ps1
# Vuforia e-posta ve şifreniz yalnızca bu pencerede sorulur; hiçbir dosyaya yazılmaz.
# Basic plan ile en fazla 20 Model Target üretilebilir.

param(
    [string]$Cli = "$env:USERPROFILE\Tools\vuforia-mt-cli\ModelTargetCLI.exe"
)

$root = Split-Path $PSScriptRoot -Parent
$json = Join-Path $root "ModelTarget\model_target.json"
$out = Join-Path $root "ModelTarget\cikti"

if (-not (Test-Path $Cli)) { Write-Host "Model Target CLI bulunamadı: $Cli" -ForegroundColor Red; Read-Host "Kapatmak için Enter"; exit 1 }
if (-not (Test-Path $json)) { Write-Host "Önce Unity'de: G6 > Vuforia > Model Target dosyalarını hazırla" -ForegroundColor Red; Read-Host "Kapatmak için Enter"; exit 1 }

$user = Read-Host "Vuforia e-posta"
$secure = Read-Host "Vuforia şifre" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $pass = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    New-Item -ItemType Directory -Force $out | Out-Null
    & $Cli -i $json -o $out -vu $user -vp $pass
    $code = $LASTEXITCODE
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    $pass = $null
}

if ($code -eq 0) { Write-Host "`nModel Target üretildi: $out" -ForegroundColor Green }
else { Write-Host "`nHata kodu: $code (100-110 arası: argüman, dosya veya imzalama hatası)" -ForegroundColor Red }
Read-Host "Kapatmak için Enter"
