[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "concertable-auth-package-verification-$([guid]::NewGuid().ToString('N'))"
$packageOutput = Join-Path $temporaryRoot 'packages'
$consumerRoot = Join-Path $temporaryRoot 'consumer'
$expectedPackageIds = @('Concertable.Auth.Contracts', 'Concertable.Auth.Hosting')
$packageProjects = @(
    (Join-Path $repositoryRoot 'Concertable.Auth.Contracts/Concertable.Auth.Contracts.csproj'),
    (Join-Path $repositoryRoot 'src/Concertable.Auth.Hosting/Concertable.Auth.Hosting.csproj')
)
$canonicalRepositoryUrl = 'https://github.com/Concertable/auth'
$minimumAuthVersionCore = [version] '0.2.0'

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

try {
    New-Item -ItemType Directory -Path $packageOutput, $consumerRoot | Out-Null

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
    $consumerProjectPath = Join-Path $consumerRoot 'AuthPackageConsumer.csproj'
    Set-Content -LiteralPath $consumerProjectPath -Value $consumerProject -Encoding utf8NoBOM

    $consumerConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="auth-local" value="$packageOutput" />
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
    $consumerConfigPath = Join-Path $consumerRoot 'nuget.config'
    Set-Content -LiteralPath $consumerConfigPath -Value $consumerConfig -Encoding utf8NoBOM

    & dotnet restore $consumerProjectPath --configfile $consumerConfigPath --packages (Join-Path $consumerRoot 'packages')
    if ($LASTEXITCODE -ne 0) {
        throw "Auth clean-consumer restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build $consumerProjectPath --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Auth clean-consumer build failed with exit code $LASTEXITCODE."
    }

    Write-Host "Verified Auth packages at version ${contractsVersion}: $($expectedPackageIds -join ', ')."
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
