[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputPath,

    [switch] $KeepArtifacts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$repositoryUrl = 'https://github.com/Concertable/auth'
$runtimeRepository = 'ghcr.io/concertable/auth'
$migrationRepository = 'ghcr.io/concertable/auth-operational-store-migration'
$trivyImage = 'aquasec/trivy:0.74.0@sha256:62b1e65e8869bc4b4c6aa4fa2b21595256c7c2f6018a9d9ad61caf87187c1969'
$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($revision)) {
    throw 'Could not resolve the Auth source revision.'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    if ($KeepArtifacts) {
        throw 'OutputPath is required when KeepArtifacts is specified.'
    }

    $releaseRoot = Join-Path `
        ([System.IO.Path]::GetTempPath()) `
        "concertable-auth-release-candidate-$([Guid]::NewGuid().ToString('N'))"
}
else {
    $releaseRoot = [System.IO.Path]::GetFullPath($OutputPath, $repositoryRoot)
}

if (Test-Path -LiteralPath $releaseRoot) {
    throw "Release-candidate output already exists: '$releaseRoot'."
}

$markerPath = Join-Path $releaseRoot '.auth-release-candidate'
$packageRoot = Join-Path $releaseRoot 'package-verification'
$imageRoot = Join-Path $releaseRoot 'images'
$evidenceRoot = Join-Path $releaseRoot 'evidence'
$manifestPath = Join-Path $releaseRoot 'release-manifest.json'
$releaseId = [Guid]::NewGuid().ToString('N')
# Persistent across runs, deliberately. A per-run cache is cold by construction on every invocation:
# Trivy re-downloads its database and re-analyses every layer, turning a ~100s scan into 10+ minutes and
# then into a timeout that looks exactly like a finding. Gitignored via artifacts/; CI caches this path.
$trivyCachePath = Join-Path $repositoryRoot 'artifacts/.trivy-cache'
$runtimeImage = "concertable/auth:release-candidate-$releaseId"
$migrationImage = "concertable/auth-operational-store-migration:release-candidate-$releaseId"
$candidateImages = [System.Collections.Generic.List[string]]::new()
$releaseRootCreated = $false
$completed = $false
$releaseVersion = ''
$packageToken = $env:GITHUB_PACKAGES_TOKEN
Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue

function Assert-ReleaseRoot {
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Refusing release-candidate operation without marker '$markerPath'."
    }
}

function Invoke-DockerBuildWithPackageToken {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [Parameter(Mandatory)]
        [string] $Token
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'docker'
    $startInfo.UseShellExecute = $false
    $startInfo.Environment['GITHUB_PACKAGES_TOKEN'] = $Token
    foreach ($argument in $Arguments) {
        $null = $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw 'Could not start Docker.'
    }

    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Docker build failed with exit code $($process.ExitCode)."
    }
}

function Get-ImageInspection {
    param([Parameter(Mandatory)][string] $Image)

    $json = & docker image inspect $Image
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect release-candidate image '$Image'."
    }

    return ($json | ConvertFrom-Json)[0]
}

function Assert-CandidateImage {
    param(
        [Parameter(Mandatory)][string] $Image,
        [Parameter(Mandatory)][string] $Version,
        [Parameter(Mandatory)][string] $ExpectedAssembly
    )

    $inspection = Get-ImageInspection -Image $Image
    if ($inspection.Config.Labels.'org.opencontainers.image.source' -ne $repositoryUrl) {
        throw "Release-candidate image '$Image' has the wrong source label."
    }
    if ($inspection.Config.Labels.'org.opencontainers.image.revision' -ne $revision) {
        throw "Release-candidate image '$Image' has the wrong source revision."
    }
    if ($inspection.Config.Labels.'org.opencontainers.image.version' -ne $Version) {
        throw "Release-candidate image '$Image' does not share package version '$Version'."
    }
    if ([string]::IsNullOrWhiteSpace([string] $inspection.Config.User) -or $inspection.Config.User -in @('0', 'root')) {
        throw "Release-candidate image '$Image' does not declare a non-root user."
    }

    $entrypoint = @($inspection.Config.Entrypoint)
    if ($entrypoint.Count -ne 2 -or $entrypoint[0] -ne 'dotnet' -or $entrypoint[1] -ne $ExpectedAssembly) {
        throw "Release-candidate image '$Image' has an unexpected entrypoint."
    }
}

function Invoke-Trivy {
    <#
        Never passes --exit-code. Trivy exits 1 for "findings present" AND for any fatal error, so under
        --exit-code the two are indistinguishable at the call site — a timeout reads exactly like a secret
        detection. The report file is the discriminator: a fatal run never writes one. Callers gate on the
        parsed report, not on the exit code.
    #>
    param(
        [Parameter(Mandatory)][string[]] $Arguments,
        [Parameter(Mandatory)][string] $ReportPath
    )

    if (Test-Path -LiteralPath $ReportPath) {
        Remove-Item -LiteralPath $ReportPath -Force
    }

    & docker run --rm `
        --volume "${repositoryRoot}:/work:ro" `
        --volume "${imageRoot}:/images:ro" `
        --volume "${evidenceRoot}:/evidence" `
        --volume "${trivyCachePath}:/root/.cache/trivy" `
        $trivyImage `
        @Arguments
    $trivyExit = $LASTEXITCODE

    if (-not (Test-Path -LiteralPath $ReportPath)) {
        throw "Trivy could not complete (exit code $trivyExit, no report written) for arguments '$($Arguments -join ' ')'. This is a tool failure, not a scan result."
    }
    if ($trivyExit -ne 0) {
        throw "Trivy wrote a report but exited $trivyExit for arguments '$($Arguments -join ' ')'. The report is not trusted."
    }

    return (Get-Content -Raw -LiteralPath $ReportPath | ConvertFrom-Json)
}

function Assert-NoSecrets {
    param(
        [Parameter(Mandatory)] $Report,
        [Parameter(Mandatory)][string] $Subject
    )

    # Results/Secrets/Vulnerabilities are ABSENT rather than null on a clean scan, and Set-StrictMode
    # throws on a missing property — so every hop is existence-checked, not null-checked.
    $results = if ($Report.PSObject.Properties.Name -contains 'Results') { @($Report.Results) } else { @() }
    $findings = @($results | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains 'Secrets') { @($_.Secrets) } })
    if ($findings.Count -gt 0) {
        throw "Secret scan found $($findings.Count) finding(s) in ${Subject}: $(($findings | ForEach-Object { $_.RuleID }) -join ', ')."
    }
}

function Assert-NoCriticalVulnerabilities {
    param(
        [Parameter(Mandatory)] $Report,
        [Parameter(Mandatory)][string] $Subject
    )

    $results = if ($Report.PSObject.Properties.Name -contains 'Results') { @($Report.Results) } else { @() }
    $findings = @($results | ForEach-Object {
        if ($_.PSObject.Properties.Name -contains 'Vulnerabilities') { @($_.Vulnerabilities) } })
    if ($findings.Count -gt 0) {
        throw "Vulnerability scan found $($findings.Count) CRITICAL finding(s) in ${Subject}: $(($findings | ForEach-Object { $_.VulnerabilityID }) -join ', ')."
    }
}

function Initialize-TrivyCache {
    [System.IO.Directory]::CreateDirectory($trivyCachePath) | Out-Null
}

function Get-NuGetIdentity {
    param([Parameter(Mandatory)][System.IO.FileInfo] $Package)

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

        return [ordered]@{
            Id = [string] $manifest.package.metadata.id
            Version = [string] $manifest.package.metadata.version
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-ArtifactRecord {
    param([Parameter(Mandatory)][string] $Path)

    $item = Get-Item -LiteralPath $Path
    return [ordered]@{
        path = [System.IO.Path]::GetRelativePath($releaseRoot, $item.FullName).Replace('\', '/')
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.FullName).Hash.ToLowerInvariant()
        bytes = $item.Length
    }
}

function Remove-CandidateImage {
    param(
        [Parameter(Mandatory)][string] $Image,
        [Parameter(Mandatory)][string] $Version
    )

    $inspectionJson = & docker image inspect $Image 2>$null
    if ($LASTEXITCODE -ne 0) {
        return
    }

    $inspection = ($inspectionJson | ConvertFrom-Json)[0]
    if ($inspection.Config.Labels.'org.opencontainers.image.source' -ne $repositoryUrl -or
        $inspection.Config.Labels.'org.opencontainers.image.revision' -ne $revision -or
        $inspection.Config.Labels.'org.opencontainers.image.version' -ne $Version) {
        throw "Refusing to remove unowned image '$Image'."
    }

    & docker image rm $Image
    if ($LASTEXITCODE -ne 0) {
        throw "Could not remove release-candidate image '$Image'."
    }
}

try {
    if ([string]::IsNullOrWhiteSpace($packageToken)) {
        throw 'GITHUB_PACKAGES_TOKEN is required for release-candidate package restore and runtime image build.'
    }

    New-Item -ItemType Directory -Path $releaseRoot | Out-Null
    $releaseRootCreated = $true
    try {
        Set-Content -LiteralPath $markerPath -Value 'Concertable.Auth release candidate' -Encoding utf8NoBOM
    }
    catch {
        Remove-Item -LiteralPath $releaseRoot -Recurse -Force
        $releaseRootCreated = $false
        throw
    }
    New-Item -ItemType Directory -Path $imageRoot, $evidenceRoot | Out-Null

    Initialize-TrivyCache

    $env:GITHUB_PACKAGES_TOKEN = $packageToken
    & (Join-Path $PSScriptRoot 'verify-auth-packages.ps1') `
        -Configuration $Configuration `
        -Phase All `
        -OutputPath $packageRoot `
        -KeepArtifacts
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue

    $version = (Get-Content -Raw -LiteralPath (Join-Path $packageRoot 'version.txt')).Trim()
    if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
        throw "Auth release-candidate version '$version' is not valid SemVer."
    }
    $releaseCoreVersion = [version] ($version.Split('-', 2)[0])
    if ($releaseCoreVersion -lt [version] '0.2.0') {
        throw "Auth release-candidate version '$version' is below the independent 0.2.0 baseline."
    }
    $releaseVersion = $version

    $cleanConsumerPath = Join-Path $evidenceRoot 'clean-consumer.json'
    [ordered]@{
        status = 'succeeded'
        version = $version
        packages = @('Concertable.Auth.Contracts', 'Concertable.Auth.Hosting')
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $cleanConsumerPath -Encoding utf8NoBOM

    $packageArtifactRoot = Join-Path $releaseRoot 'packages'
    Move-Item -LiteralPath (Join-Path $packageRoot 'packages') -Destination $packageArtifactRoot
    $packageMarker = Join-Path $packageRoot '.auth-package-verification'
    if (-not (Test-Path -LiteralPath $packageMarker -PathType Leaf)) {
        throw "Refusing to clean package verification without marker '$packageMarker'."
    }
    Remove-Item -LiteralPath $packageRoot -Recurse -Force

    $env:GITHUB_PACKAGES_TOKEN = $packageToken
    & (Join-Path $PSScriptRoot 'verify-auth-images.ps1') -BuildVersion $version
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue

    foreach ($image in @($runtimeImage, $migrationImage)) {
        $existingIds = @(& docker image ls --quiet --no-trunc --filter "reference=$image")
        if ($LASTEXITCODE -ne 0) {
            throw "Could not check release-candidate image tag '$image'."
        }
        if ($existingIds.Count -gt 0) {
            throw "Refusing to overwrite release-candidate image tag '$image'."
        }
    }

    $commonArguments = @(
        'build',
        '--file', (Join-Path $repositoryRoot 'Dockerfile'),
        '--build-arg', "VCS_REF=$revision",
        '--build-arg', "BUILD_VERSION=$version",
        '--pull'
    )
    Invoke-DockerBuildWithPackageToken `
        -Token $packageToken `
        -Arguments ($commonArguments + @(
            '--secret', 'id=github_packages_token,env=GITHUB_PACKAGES_TOKEN',
            '--target', 'auth-runtime',
            '--tag', $runtimeImage,
            $repositoryRoot
        ))
    $candidateImages.Add($runtimeImage)
    $packageToken = $null

    $migrationArguments = $commonArguments + @(
        '--target', 'operational-store-migration',
        '--tag', $migrationImage,
        $repositoryRoot
    )
    & docker @migrationArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Migration release-candidate build failed with exit code $LASTEXITCODE."
    }
    $candidateImages.Add($migrationImage)

    Assert-CandidateImage -Image $runtimeImage -Version $version -ExpectedAssembly 'Concertable.Auth.dll'
    Assert-CandidateImage -Image $migrationImage -Version $version -ExpectedAssembly 'Concertable.Auth.OperationalStoreMigration.dll'

    $sourceSecretReport = Invoke-Trivy -ReportPath (Join-Path $evidenceRoot 'source-secrets.json') -Arguments @(
        'filesystem', '--scanners', 'secret', '--format', 'json',
        '--output', '/evidence/source-secrets.json', '--no-progress', '--timeout', '30m', '/work'
    )
    Assert-NoSecrets -Report $sourceSecretReport -Subject 'the repository source'

    $imageEvidence = @(
        [ordered]@{
            image = $runtimeImage
            repository = $runtimeRepository
            archive = Join-Path $imageRoot 'auth-runtime.tar'
            sbom = Join-Path $evidenceRoot 'auth-runtime.cdx.json'
            vulnerabilities = Join-Path $evidenceRoot 'auth-runtime-vulnerabilities.json'
            secrets = Join-Path $evidenceRoot 'auth-runtime-secrets.json'
        },
        [ordered]@{
            image = $migrationImage
            repository = $migrationRepository
            archive = Join-Path $imageRoot 'auth-operational-store-migration.tar'
            sbom = Join-Path $evidenceRoot 'auth-operational-store-migration.cdx.json'
            vulnerabilities = Join-Path $evidenceRoot 'auth-operational-store-migration-vulnerabilities.json'
            secrets = Join-Path $evidenceRoot 'auth-operational-store-migration-secrets.json'
        }
    )

    foreach ($item in $imageEvidence) {
        & docker image save --output $item.archive $item.image
        if ($LASTEXITCODE -ne 0 -or (Get-Item -LiteralPath $item.archive).Length -eq 0) {
            throw "Could not save release-candidate image '$($item.image)'."
        }

        $archiveFile = [System.IO.Path]::GetFileName($item.archive)
        $vulnerabilityFile = [System.IO.Path]::GetFileName($item.vulnerabilities)
        $secretFile = [System.IO.Path]::GetFileName($item.secrets)
        $sbomFile = [System.IO.Path]::GetFileName($item.sbom)
        $vulnerabilityReport = Invoke-Trivy -ReportPath $item.vulnerabilities -Arguments @(
            'image', '--scanners', 'vuln', '--severity', 'CRITICAL', '--format', 'json',
            '--output', "/evidence/$vulnerabilityFile", '--no-progress', '--timeout', '30m',
            '--input', "/images/$archiveFile"
        )
        Assert-NoCriticalVulnerabilities -Report $vulnerabilityReport -Subject $item.image

        $imageSecretReport = Invoke-Trivy -ReportPath $item.secrets -Arguments @(
            'image', '--scanners', 'secret', '--format', 'json',
            '--output', "/evidence/$secretFile", '--no-progress', '--timeout', '30m',
            '--input', "/images/$archiveFile"
        )
        Assert-NoSecrets -Report $imageSecretReport -Subject $item.image

        $sbom = Invoke-Trivy -ReportPath $item.sbom -Arguments @(
            'image', '--format', 'cyclonedx', '--output', "/evidence/$sbomFile", '--no-progress',
            '--timeout', '30m', '--input', "/images/$archiveFile"
        )
        if ($sbom.bomFormat -ne 'CycloneDX' -or @($sbom.components).Count -eq 0) {
            throw "Release-candidate SBOM validation failed for '$($item.image)'."
        }
    }

    $packages = @(Get-ChildItem -LiteralPath $packageArtifactRoot -Filter '*.nupkg' -File |
        Where-Object Name -NotLike '*.symbols.nupkg')
    $packageRecords = @($packages | ForEach-Object {
        $identity = Get-NuGetIdentity -Package $_
        if ($identity.Version -ne $version) {
            throw "Package '$($identity.Id)' version '$($identity.Version)' does not match '$version'."
        }

        [ordered]@{
            id = $identity.Id
            version = $identity.Version
            artifact = Get-ArtifactRecord -Path $_.FullName
        }
    } | Sort-Object id)
    if (($packageRecords.id -join ',') -ne 'Concertable.Auth.Contracts,Concertable.Auth.Hosting') {
        throw "Unexpected Auth release-candidate package set '$($packageRecords.id -join ',')'."
    }

    $imageRecords = @($imageEvidence | ForEach-Object {
        $inspection = Get-ImageInspection -Image $_.image
        [ordered]@{
            repository = $_.repository
            version = $version
            sourceRevision = $revision
            intendedTags = @($version, $revision)
            localImageId = [string] $inspection.Id
            archive = Get-ArtifactRecord -Path $_.archive
            sbom = Get-ArtifactRecord -Path $_.sbom
            criticalVulnerabilityScan = Get-ArtifactRecord -Path $_.vulnerabilities
            allSeveritySecretScan = Get-ArtifactRecord -Path $_.secrets
        }
    })

    $manifest = [ordered]@{
        schemaVersion = 1
        repository = $repositoryUrl
        sourceRevision = $revision
        version = $version
        packages = $packageRecords
        images = $imageRecords
        cleanConsumer = Get-ArtifactRecord -Path $cleanConsumerPath
        sourceSecretScan = Get-ArtifactRecord -Path (Join-Path $evidenceRoot 'source-secrets.json')
    }
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

    $verifiedManifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    if ($verifiedManifest.sourceRevision -ne $revision -or
        $verifiedManifest.version -ne $version -or
        @($verifiedManifest.packages).Count -ne 2 -or
        @($verifiedManifest.images).Count -ne 2) {
        throw 'Release-candidate manifest validation failed.'
    }

    $expectedArtifactPaths = @(
        $packageRecords.artifact.path
        $imageRecords.archive.path
        $imageRecords.sbom.path
        $imageRecords.criticalVulnerabilityScan.path
        $imageRecords.allSeveritySecretScan.path
        $manifest.cleanConsumer.path
        $manifest.sourceSecretScan.path
    ) | Sort-Object
    $actualArtifactPaths = @(Get-ChildItem -LiteralPath $releaseRoot -Force -Recurse -File |
        Where-Object FullName -NotIn @($markerPath, $manifestPath) |
        ForEach-Object { [System.IO.Path]::GetRelativePath($releaseRoot, $_.FullName).Replace('\', '/') } |
        Sort-Object)
    if (($actualArtifactPaths -join "`n") -ne ($expectedArtifactPaths -join "`n")) {
        throw "Release-candidate bundle contains unmanifested files: '$($actualArtifactPaths -join ', ')'."
    }

    $completed = $true
    Write-Host "Verified Auth release candidate $version for revision ${revision}: 2 packages, 2 images, manifest and evidence complete."
    if ($KeepArtifacts) {
        Write-Host "Retained release-candidate artifacts at '$releaseRoot'."
    }
}
finally {
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    $packageToken = $null

    try {
        if (-not [string]::IsNullOrWhiteSpace($releaseVersion)) {
            foreach ($image in $candidateImages) {
                Remove-CandidateImage -Image $image -Version $releaseVersion
            }
        }
    }
    finally {
        # The Trivy cache deliberately survives the run; it is the whole point of a warm cache.
        if ((Test-Path -LiteralPath $markerPath -PathType Leaf) -and (-not $KeepArtifacts -or -not $completed)) {
            Assert-ReleaseRoot
            Remove-Item -LiteralPath $releaseRoot -Recurse -Force
            $releaseRootCreated = $false
        }
        elseif ($releaseRootCreated -and -not (Test-Path -LiteralPath $markerPath)) {
            Remove-Item -LiteralPath $releaseRoot -Recurse -Force -ErrorAction SilentlyContinue
            $releaseRootCreated = $false
        }
    }
}
