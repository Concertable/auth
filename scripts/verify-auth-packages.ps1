[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [ValidateSet('All', 'Prepare', 'Complete')]
    [string] $Phase = 'All',

    [string] $OutputPath,

    [switch] $KeepArtifacts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    if ($KeepArtifacts) {
        throw 'OutputPath is required when KeepArtifacts is specified.'
    }

    if ($Phase -ne 'All') {
        throw 'OutputPath is required for the Prepare and Complete phases.'
    }

    $verificationRoot = Join-Path `
        ([System.IO.Path]::GetTempPath()) `
        "concertable-auth-package-verification-$([guid]::NewGuid().ToString('N'))"
}
else {
    $verificationRoot = [System.IO.Path]::GetFullPath($OutputPath, $repositoryRoot)
}

$markerPath = Join-Path $verificationRoot '.auth-package-verification'
$packageOutput = Join-Path $verificationRoot 'packages'
$consumerRoot = Join-Path $verificationRoot 'consumer'
$consumerProjectPath = Join-Path $consumerRoot 'AuthPackageConsumer.csproj'
$consumerConfigPath = Join-Path $consumerRoot 'nuget.config'
$consumerPackages = Join-Path $consumerRoot 'packages'
$versionPath = Join-Path $verificationRoot 'version.txt'
$expectedPackageIds = @('Concertable.Auth.Contracts', 'Concertable.Auth.Hosting')
$packageProjects = @(
    (Join-Path $repositoryRoot 'Concertable.Auth.Contracts/Concertable.Auth.Contracts.csproj'),
    (Join-Path $repositoryRoot 'src/Concertable.Auth.Hosting/Concertable.Auth.Hosting.csproj')
)
$canonicalRepositoryUrl = 'https://github.com/Concertable/auth'
$minimumAuthVersionCore = [version] '0.2.0'
$packageToken = $env:GITHUB_PACKAGES_TOKEN
Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue

function Assert-Equal {
    param(
        [Parameter(Mandatory)] $Actual,
        [Parameter(Mandatory)] $Expected,
        [Parameter(Mandatory)] [string] $Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message Expected '$Expected', found '$Actual'."
    }
}

function Assert-VerificationRoot {
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Refusing package verification operation without marker '$markerPath'."
    }
}

function Invoke-PackageRestore {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Token,
        [Parameter(Mandatory)] [string] $Description
    )

    if ([string]::IsNullOrWhiteSpace($Token)) {
        throw 'GITHUB_PACKAGES_TOKEN is required for authenticated package restore.'
    }

    try {
        $env:GITHUB_PACKAGES_TOKEN = $Token
        & dotnet restore @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$Description failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    }
}

function Get-PackageManifest {
    param([Parameter(Mandatory)] [System.IO.FileInfo] $Package)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Package.FullName)
    try {
        $manifestEntry = $archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1
        if ($null -eq $manifestEntry) {
            throw "Package '$($Package.Name)' does not contain a NuGet manifest."
        }

        $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
        try {
            [xml] $manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $readmeEntry = $archive.Entries | Where-Object FullName -EQ 'README.md' | Select-Object -First 1
        if ($null -eq $readmeEntry) {
            throw "Package '$($Package.Name)' does not contain README.md."
        }

        return $manifest
    }
    finally {
        $archive.Dispose()
    }
}

function Prepare-PackageVerification {
    if (Test-Path -LiteralPath $verificationRoot) {
        throw "Package verification output already exists: '$verificationRoot'."
    }

    New-Item -ItemType Directory -Path $packageOutput, $consumerRoot | Out-Null
    Set-Content -LiteralPath $markerPath -Value 'Concertable.Auth package verification' -Encoding utf8NoBOM

    try {
        foreach ($packageProject in $packageProjects) {
            & dotnet pack $packageProject --configuration $Configuration --no-build --no-restore --output $packageOutput
            if ($LASTEXITCODE -ne 0) {
                throw "Auth package creation failed for '$packageProject' with exit code $LASTEXITCODE."
            }
        }

        $packages = @(Get-ChildItem -LiteralPath $packageOutput -Filter '*.nupkg' -File |
            Where-Object Name -NotLike '*.symbols.nupkg')
        $actualPackageIds = @($packages | ForEach-Object {
            $manifest = Get-PackageManifest -Package $_
            [string] $manifest.package.metadata.id
        } | Sort-Object)

        Assert-Equal -Actual $packages.Count -Expected $expectedPackageIds.Count -Message 'Unexpected package count.'
        Assert-Equal -Actual ($actualPackageIds -join ',') -Expected (($expectedPackageIds | Sort-Object) -join ',') -Message 'Unexpected Auth package set.'

        $manifestsById = @{}
        foreach ($package in $packages) {
            $manifest = Get-PackageManifest -Package $package
            $metadata = $manifest.package.metadata
            $packageId = [string] $metadata.id
            $manifestsById[$packageId] = $manifest

            Assert-Equal -Actual ([string] $metadata.repository.url) -Expected $canonicalRepositoryUrl -Message "$packageId repository URL is not canonical."
            Assert-Equal -Actual ([string] $metadata.projectUrl) -Expected $canonicalRepositoryUrl -Message "$packageId project URL is not canonical."
            Assert-Equal -Actual ([string] $metadata.readme) -Expected 'README.md' -Message "$packageId package readme is not declared."
            if ([string]::IsNullOrWhiteSpace([string] $metadata.description) -or [string] $metadata.description -eq 'Package Description') {
                throw "$packageId does not have a meaningful package description."
            }
        }

        $contractsVersion = [string] $manifestsById['Concertable.Auth.Contracts'].package.metadata.version
        $hostingMetadata = $manifestsById['Concertable.Auth.Hosting'].package.metadata
        Assert-Equal -Actual ([string] $hostingMetadata.version) -Expected $contractsVersion -Message 'Auth packages do not share one release version.'
        $authVersionCore = [version] ($contractsVersion.Split('-', 2)[0])
        if ($authVersionCore -lt $minimumAuthVersionCore) {
            throw "Auth package version '$contractsVersion' does not clear the retained 0.1.0-alpha.0.1283 high-water mark."
        }

        $contractsDependency = @($hostingMetadata.dependencies.group.dependency) |
            Where-Object id -EQ 'Concertable.Auth.Contracts' |
            Select-Object -First 1
        if ($null -eq $contractsDependency) {
            throw 'Concertable.Auth.Hosting does not declare its Auth.Contracts dependency.'
        }
        Assert-Equal -Actual ([string] $contractsDependency.version) -Expected $contractsVersion -Message 'Auth.Hosting does not depend on the same Auth.Contracts release.'

        $consumerProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Concertable.Auth.Contracts" Version="$contractsVersion" />
    <PackageReference Include="Concertable.Auth.Hosting" Version="$contractsVersion" />
  </ItemGroup>
</Project>
"@
        Set-Content -LiteralPath $consumerProjectPath -Value $consumerProject -Encoding utf8NoBOM

        $escapedPackageOutput = [System.Security.SecurityElement]::Escape($packageOutput)
        $consumerConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="auth-local" value="$escapedPackageOutput" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/Concertable/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="auth-local">
      <package pattern="Concertable.Auth.Contracts" />
      <package pattern="Concertable.Auth.Hosting" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="github">
      <package pattern="Concertable.*" />
    </packageSource>
  </packageSourceMapping>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="Concertable" />
      <add key="ClearTextPassword" value="%GITHUB_PACKAGES_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
"@
        Set-Content -LiteralPath $consumerConfigPath -Value $consumerConfig -Encoding utf8NoBOM
        Set-Content -LiteralPath $versionPath -Value $contractsVersion -Encoding utf8NoBOM

        Write-Host "Prepared Auth packages at version ${contractsVersion}: $($expectedPackageIds -join ', ')."
    }
    catch {
        Assert-VerificationRoot
        Remove-Item -LiteralPath $verificationRoot -Recurse -Force
        throw
    }
}

function Complete-PackageVerification {
    Assert-VerificationRoot
    $contractsVersion = (Get-Content -Raw -LiteralPath $versionPath).Trim()

    try {
        & dotnet build $consumerProjectPath --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Auth clean-consumer build failed with exit code $LASTEXITCODE."
        }

        Write-Host "Verified Auth packages at version ${contractsVersion}: $($expectedPackageIds -join ', ')."
    }
    finally {
        if (-not $KeepArtifacts) {
            Remove-Item -LiteralPath $verificationRoot -Recurse -Force
        }
    }
}

try {
    if ($Phase -ne 'All' -and -not [string]::IsNullOrWhiteSpace($packageToken)) {
        throw "The $Phase phase must not receive GITHUB_PACKAGES_TOKEN."
    }

    switch ($Phase) {
        'All' {
            foreach ($packageProject in $packageProjects) {
                Invoke-PackageRestore `
                    -Arguments @($packageProject, '--force-evaluate') `
                    -Token $packageToken `
                    -Description "Auth package-project restore for '$packageProject'"
            }
            foreach ($packageProject in $packageProjects) {
                & dotnet build $packageProject --configuration $Configuration --no-restore
                if ($LASTEXITCODE -ne 0) {
                    throw "Auth package-project build failed for '$packageProject' with exit code $LASTEXITCODE."
                }
            }

            Prepare-PackageVerification
            Invoke-PackageRestore `
                -Arguments @($consumerProjectPath, '--configfile', $consumerConfigPath, '--packages', $consumerPackages) `
                -Token $packageToken `
                -Description 'Auth clean-consumer restore'
            $packageToken = $null
            Complete-PackageVerification
        }
        'Prepare' {
            Prepare-PackageVerification
        }
        'Complete' {
            Complete-PackageVerification
        }
    }
}
catch {
    if (Test-Path -LiteralPath $markerPath -PathType Leaf) {
        Remove-Item -LiteralPath $verificationRoot -Recurse -Force
    }
    throw
}
finally {
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    $packageToken = $null
}
