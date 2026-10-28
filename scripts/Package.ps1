param(
    [string]$SptRoot = 'C:\Games\SPT-4.1',
    [string]$SainDirectory = '',
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $SainDirectory) { $SainDirectory = Join-Path $SptRoot 'BepInEx\plugins\SAIN' }
if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'SAINPersonalityMixer.csproj') -c Release --nologo "-p:SptRoot=$SptRoot" "-p:SainDirectory=$SainDirectory"
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
}
$dll = Join-Path $root 'bin\Release\netstandard2.1\SAINPersonalityMixer.dll'
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion.Split('+')[0]
if ($version -ne '1.0.0') { throw "Unexpected release version: $version" }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$path = Join-Path $dist "SAIN-Personality-Mixer-$version.zip"
$stream = [System.IO.File]::Open($path, [System.IO.FileMode]::Create)
$zip = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $files = [ordered]@{
        'BepInEx/plugins/SAINPersonalityMixer/SAINPersonalityMixer.dll' = $dll
        'README-SAIN-Personality-Mixer.md' = (Join-Path $root 'README.md')
        'CHANGELOG-SAIN-Personality-Mixer.md' = (Join-Path $root 'CHANGELOG.md')
        'LICENSE' = (Join-Path $root 'LICENSE')
    }
    foreach ($entry in $files.GetEnumerator()) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose(); $stream.Dispose() }
$verify = [System.IO.Compression.ZipFile]::OpenRead($path)
try {
    $expected = @($files.Keys | Sort-Object)
    $actual = @($verify.Entries.FullName | Sort-Object)
    if (Compare-Object $expected $actual) { throw 'Unexpected archive contents.' }
    $dllEntry = $verify.GetEntry('BepInEx/plugins/SAINPersonalityMixer/SAINPersonalityMixer.dll')
    $entryStream = $dllEntry.Open()
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { $zipDllHash = [Convert]::ToHexString($sha.ComputeHash($entryStream)) }
        finally { $sha.Dispose() }
    } finally { $entryStream.Dispose() }
    $dllHash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    if ($dllHash -ne $zipDllHash) { throw 'Packaged DLL hash mismatch.' }
} finally { $verify.Dispose() }
$manifest = [ordered]@{
    version = $version
    createdUtc = [DateTime]::UtcNow.ToString('o')
    package = $path
    packageSha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    dllSha256 = $dllHash
    dependenciesBundled = $false
    liveRaidValidated = $false
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dist 'manifest.json') -Encoding utf8
$manifest | ConvertTo-Json
