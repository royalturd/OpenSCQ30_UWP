# Builds a signed Windows Phone 8.1 .appx without Visual Studio 2015.
#
# Needs:
#   - Any Visual Studio 2017+ (or Build Tools) for the Roslyn C# compiler (csc.exe)
#   - Windows Phone 8.1 SDK (reference assemblies and Windows.winmd)
#   - Windows 8.1 SDK (makepri.exe, makeappx.exe) and a Windows 10 SDK (signtool.exe)
#
# Output: out\SCQ30Mobile_wp81.appx and out\SCQ30Mobile_wp81.cer
#
# Usage: powershell -ExecutionPolicy Bypass -File build.ps1 [-Configuration Debug|Release]

param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$name = 'SCQ30Mobile'
$publisher = 'CN=Royalturd'   # must match Identity Publisher in Package.appxmanifest
$x86 = ${env:ProgramFiles(x86)}

function Find-Tool([string]$what, [string[]]$candidates) {
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }
    throw "$what not found. Looked in:`n  " + ($candidates -join "`n  ")
}

function Invoke-Tool([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path $exe -Leaf) failed with exit code $LASTEXITCODE" }
}

# ---- tools ----

$vswhere = "$x86\Microsoft Visual Studio\Installer\vswhere.exe"
$vsPath = if (Test-Path $vswhere) { & $vswhere -latest -products * -property installationPath } else { $null }
$csc = Find-Tool 'csc.exe' @("$vsPath\MSBuild\Current\Bin\Roslyn\csc.exe", "$vsPath\MSBuild\15.0\Bin\Roslyn\csc.exe")

$refDir = Find-Tool 'Windows Phone 8.1 reference assemblies' @("$x86\Reference Assemblies\Microsoft\Framework\WindowsPhoneApp\v8.1")
$winmd = Find-Tool 'Windows Phone 8.1 Windows.winmd' @("$x86\Windows Phone Kits\8.1\References\CommonConfiguration\Neutral\Windows.winmd")
$makepri = Find-Tool 'makepri.exe (Windows 8.1 SDK)' @("$x86\Windows Kits\8.1\bin\x64\makepri.exe", "$x86\Windows Kits\8.1\bin\x86\makepri.exe")
$makeappx = Find-Tool 'makeappx.exe (Windows 8.1 SDK)' @("$x86\Windows Kits\8.1\bin\x64\makeappx.exe", "$x86\Windows Kits\8.1\bin\x86\makeappx.exe")
$signtool = Find-Tool 'signtool.exe (Windows 10 SDK)' (
    @(Get-ChildItem "$x86\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | ForEach-Object FullName) + "$x86\Windows Kits\8.1\bin\x64\signtool.exe")

# ---- folders ----

$obj = Join-Path $root "obj\wp81\$Configuration"
$stage = Join-Path $obj 'stage'
$out = Join-Path $root 'out'
if (Test-Path $obj) { Remove-Item $obj -Recurse -Force }
New-Item -ItemType Directory -Force $stage, "$stage\Assets", $out | Out-Null

# ---- compile ----

Write-Host "compiling ($Configuration)"
$sources = 'App.cs', 'MainPage.xaml.cs', 'MainPage.Ui.cs', 'SoundcoreClient.cs', 'AppLog.cs',
           'Protocol.cs', 'Prefs.cs', 'LiveTile.cs', 'Properties\AssemblyInfo.cs' |
           ForEach-Object { Join-Path $root $_ }
$cscArgs = @('/nologo', '/noconfig', '/nostdlib+', '/langversion:5', '/t:appcontainerexe', '/platform:anycpu',
             '/utf8output', "/out:$stage\$name.exe", "/resource:$root\MainPage.xaml,$name.MainPage.xaml",
             "/r:$winmd") +
           (Get-ChildItem "$refDir\*.dll" | ForEach-Object { "/r:$($_.FullName)" })
if ($Configuration -eq 'Debug') { $cscArgs += '/debug:full', '/define:DEBUG;TRACE;NETFX_CORE;WINDOWS_PHONE_APP' }
else { $cscArgs += '/optimize+', '/debug:pdbonly', '/define:TRACE;NETFX_CORE;WINDOWS_PHONE_APP' }
Invoke-Tool $csc ($cscArgs + $sources)

# ---- package layout ----

Copy-Item "$root\Assets\*.png" "$stage\Assets"
$manifest = Get-Content "$root\Package.appxmanifest" -Raw
$manifest = $manifest.Replace('$targetnametoken$', $name).Replace('Language="x-generate"', 'Language="en-US"')
[IO.File]::WriteAllText("$stage\AppxManifest.xml", $manifest, (New-Object Text.UTF8Encoding $false))

Write-Host 'indexing resources'
$priConfig = Join-Path $obj 'priconfig.xml'
Invoke-Tool $makepri @('createconfig', '/cf', $priConfig, '/dq', 'en-US', '/o')
Invoke-Tool $makepri @('new', '/pr', $stage, '/cf', $priConfig, '/mn', "$stage\AppxManifest.xml",
                       '/of', "$stage\resources.pri", '/o')

Write-Host 'packing'
$appx = Join-Path $out "${name}_wp81.appx"
Invoke-Tool $makeappx @('pack', '/d', $stage, '/p', $appx, '/o')

# ---- sign ----
# A self-signed certificate in the user store, created once. The phone does not need it trusted
# for developer deployment; the .cer is exported in case a tool asks for it.

$cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $publisher -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date) } |
        Select-Object -First 1
if (-not $cert) {
    Write-Host "creating self-signed certificate $publisher"
    $cert = New-SelfSignedCertificate -Type Custom -Subject $publisher -KeyUsage DigitalSignature `
        -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5) `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
}
Write-Host 'signing'
Invoke-Tool $signtool @('sign', '/q', '/fd', 'SHA256', '/sha1', $cert.Thumbprint, '/s', 'My', $appx)
Export-Certificate -Cert $cert -FilePath (Join-Path $out "${name}_wp81.cer") | Out-Null

Write-Host "done: $appx"
