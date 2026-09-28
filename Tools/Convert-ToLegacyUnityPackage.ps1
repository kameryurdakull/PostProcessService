param(
    [Parameter(Mandatory = $true)]
    [string]$SourceArchive,

    [Parameter(Mandatory = $true)]
    [string]$DestinationArchive
)

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$stage = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'Temp/PostProcessLegacyPackageStage'))
$expectedPrefix = $projectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $stage.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'The staging directory must remain inside the project.'
}

$source = [System.IO.Path]::GetFullPath($SourceArchive)
$destination = [System.IO.Path]::GetFullPath($DestinationArchive)
if (-not [System.IO.File]::Exists($source)) {
    throw "Source archive does not exist: $source"
}

if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}
New-Item -ItemType Directory -Path $stage | Out-Null

try {
    & tar -xzf $source -C $stage
    if ($LASTEXITCODE -ne 0) { throw 'Could not extract the Unity package.' }

    $packagePrefix = 'Packages/com.kameryurdakull.post-process-service'
    $assetPrefix = 'Assets/PostProcessService'
    $entryCount = 0
    foreach ($directory in Get-ChildItem -LiteralPath $stage -Directory) {
        $pathnameFile = Join-Path $directory.FullName 'pathname'
        if (-not (Test-Path -LiteralPath $pathnameFile)) { continue }

        $originalPath = [System.IO.File]::ReadAllText($pathnameFile)
        if (-not $originalPath.StartsWith($packagePrefix, [System.StringComparison]::Ordinal)) {
            throw "Unexpected asset path in archive: $originalPath"
        }

        if ($originalPath -eq "$packagePrefix/package.json") {
            Remove-Item -LiteralPath $directory.FullName -Recurse -Force
            continue
        }

        $assetPath = $assetPrefix + $originalPath.Substring($packagePrefix.Length)
        [System.IO.File]::WriteAllText($pathnameFile, $assetPath, [System.Text.Encoding]::ASCII)
        $entryCount++
    }

    if ($entryCount -lt 10) { throw "Archive contains too few assets: $entryCount" }

    $destinationDirectory = [System.IO.Path]::GetDirectoryName($destination)
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
    & tar -czf $destination -C $stage .
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the legacy Unity package.' }

    Write-Output "Created $destination with $entryCount assets."
}
finally {
    if (Test-Path -LiteralPath $stage) {
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
}
