param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    dotnet restore Code/Blumind.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }
    dotnet build Code/Blumind.sln -c Release --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if (-not $SkipTests) {
        dotnet Code/Blumind.Tests/bin/Release/net10.0-windows/Blumind.Tests.dll $projectRoot
        if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
    }
    $destination = Join-Path $projectRoot 'artifacts/Blumind-net10-win-x64'
    dotnet publish Code/Blumind/Blumind.csproj -c Release -r win-x64 --self-contained true --no-restore -o $destination
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $destination
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Documents/Modernisierung-Testversion.md') -Destination (Join-Path $destination 'Liesmich.md')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts/Start-Portable.cmd') -Destination $destination
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Documents/Blumind Quick Help.bmd') -Destination $destination
    New-Item -ItemType Directory -Path (Join-Path $destination 'Icons') -Force | Out-Null
    Copy-Item -Path (Join-Path $projectRoot 'Install/Resources/Icons/*') -Destination (Join-Path $destination 'Icons')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Code/Blumind/Resources/Icons/document.ico') -Destination $destination
    $licenseDirectory = Join-Path $destination 'Licenses'
    New-Item -ItemType Directory -Path $licenseDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Documents/ThirdParty/PDFsharp.LICENSE.txt') -Destination $licenseDirectory
    $packageCache = ((& dotnet nuget locals global-packages --list) -split ': ', 2)[1].Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot locate NuGet package licenses.' }
    $packageDirectories = @()
    $lockedPackages = Get-Content -LiteralPath 'Code/Blumind/packages.lock.json' -Raw | ConvertFrom-Json
    foreach ($framework in $lockedPackages.dependencies.PSObject.Properties) {
        foreach ($package in $framework.Value.PSObject.Properties) {
            $packageDirectories += Join-Path $packageCache ($package.Name.ToLowerInvariant() + '/' + $package.Value.resolved)
        }
    }
    $runtimeConfig = Get-Content -LiteralPath (Join-Path $destination 'Blumind.runtimeconfig.json') -Raw | ConvertFrom-Json
    foreach ($framework in $runtimeConfig.runtimeOptions.includedFrameworks) {
        $packageDirectories += Join-Path $packageCache ($framework.name.ToLowerInvariant() + '.runtime.win-x64/' + $framework.version)
    }
    foreach ($packageDirectory in ($packageDirectories | Select-Object -Unique)) {
        $packageName = Split-Path -Leaf (Split-Path -Parent $packageDirectory)
        $packageLicenseDirectory = Join-Path $licenseDirectory $packageName
        New-Item -ItemType Directory -Path $packageLicenseDirectory -Force | Out-Null
        Get-ChildItem -LiteralPath $packageDirectory -File | Where-Object { $_.Name -match '^(LICENSE|NOTICE|THIRD-PARTY-NOTICES)(\..*)?$' } | Copy-Item -Destination $packageLicenseDirectory
    }
    Compress-Archive -Path (Join-Path $destination '*') -DestinationPath (Join-Path $projectRoot 'artifacts/Blumind-net10-win-x64.zip') -Force
    Write-Host "Portable build: $destination"
}
finally { Pop-Location }
