param(
    [Parameter(Mandatory)][string]$EditorDirectory,
    [string]$Bundle = 'Builds/Android/Validation/ChronosChef-4.aab'
)
$ErrorActionPreference = 'Stop'
$bundlePath = (Resolve-Path -LiteralPath $Bundle).Path
$android = Join-Path $EditorDirectory 'Data/PlaybackEngines/AndroidPlayer'
$java = Join-Path $android 'OpenJDK/bin/java.exe'
$bundleTool = (Get-ChildItem -LiteralPath (Join-Path $android 'Tools') -Filter 'bundletool-all-*.jar' | Select-Object -First 1).FullName
if (!$bundleTool) { throw 'bundletool not found in the Android module.' }
function Invoke-BundleTool([string[]]$Arguments) {
    $lines = & $java -jar $bundleTool @Arguments "--bundle=$bundlePath"
    if ($LASTEXITCODE -ne 0) { throw "bundletool failed ($LASTEXITCODE)." }
    return $lines -join [Environment]::NewLine
}
$null = Invoke-BundleTool @('validate')
[xml]$manifest = Invoke-BundleTool @('dump', 'manifest', '--module=base')
$androidXml = 'http://schemas.android.com/apk/res/android'
$target = [int]$manifest.manifest.'uses-sdk'.GetAttribute('targetSdkVersion', $androidXml)
if ($target -lt 36) { throw "Unexpected target SDK $target" }
if ($manifest.manifest.application.GetAttribute('debuggable', $androidXml) -eq 'true') { throw 'Debuggable manifest.' }
if ($manifest.manifest.package -ne 'com.chronoschef.game') { throw 'Unexpected application ID.' }
$config = Invoke-BundleTool @('dump', 'config')
if ($config -notmatch 'PAGE_ALIGNMENT_16K') { throw 'Bundle does not request 16 KB page alignment.' }
$archive = [IO.Compression.ZipFile]::OpenRead($bundlePath)
try {
    $libraries = @($archive.Entries | Where-Object FullName -match '/lib/arm64-v8a/[^/]+\.so$')
    if (!$libraries.Count) { throw 'Missing ARM64 native libraries.' }
    foreach ($library in $libraries) {
        $stream = $library.Open()
        try {
            $reader = [IO.BinaryReader]::new($stream)
            $header = $reader.ReadBytes(64)
            if ($header[4] -ne 2 -or $header[5] -ne 1) { throw "Expected 64-bit little-endian ELF: $($library.FullName)" }
            $offset = [BitConverter]::ToUInt64($header,32)
            $stride = [BitConverter]::ToUInt16($header,54)
            $count = [BitConverter]::ToUInt16($header,56)
            if ($offset -lt 64 -or $offset -gt 1048576 -or $stride -lt 56 -or !$count) { throw 'Invalid program header table.' }
            $null = $reader.ReadBytes([int]($offset - 64))
            for ($i = 0; $i -lt $count; $i++) {
                $record = $reader.ReadBytes($stride)
                if ([BitConverter]::ToUInt32($record,0) -eq 1 -and [BitConverter]::ToUInt64($record,48) -lt 16384) {
                    throw "4 KB-only load segment in $($library.FullName)"
                }
            }
        } finally { $stream.Dispose() }
    }
    [pscustomobject]@{
        Package=$manifest.manifest.package
        Version=$manifest.manifest.GetAttribute('versionName', $androidXml)
        TargetApi=$target
        Arm64Libraries=$libraries.Count
        PageAlignment='16 KB'
        SizeMiB=[math]::Round((Get-Item -LiteralPath $bundlePath).Length / 1MB,1)
        SHA256=(Get-FileHash -LiteralPath $bundlePath).Hash
    }
} finally { $archive.Dispose() }
