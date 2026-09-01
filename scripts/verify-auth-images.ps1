[CmdletBinding()]
param(
    [string] $RuntimeImage = "concertable/auth:verification-$PID",
    [string] $MigrationImage = "concertable/auth-operational-store-migration:verification-$PID",
    [switch] $KeepImages
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$repositoryUrl = 'https://github.com/Concertable/auth'
$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($revision)) {
    throw 'Could not resolve the Auth source revision.'
}
$buildVersion = "0.0.0-local.$($revision.Substring(0, 12))"

$packageToken = $env:GITHUB_PACKAGES_TOKEN
Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
if ([string]::IsNullOrWhiteSpace($packageToken)) {
    throw 'GITHUB_PACKAGES_TOKEN is required to restore Auth runtime packages during the image build.'
}

$verificationId = [Guid]::NewGuid().ToString('N')
$builtImages = [System.Collections.Generic.List[string]]::new()

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
        throw "Could not inspect image '$Image'."
    }

    return ($json | ConvertFrom-Json)[0]
}

function Assert-ImageMetadata {
    param(
        [Parameter(Mandatory)]
        [string] $Image,

        [Parameter(Mandatory)]
        [string] $ExpectedAssembly
    )

    $inspection = Get-ImageInspection -Image $Image
    if ([string]::IsNullOrWhiteSpace([string] $inspection.Config.User) -or $inspection.Config.User -in @('0', 'root')) {
        throw "Image '$Image' does not declare a non-root user."
    }

    $entrypoint = @($inspection.Config.Entrypoint)
    if ($entrypoint.Count -ne 2 -or $entrypoint[0] -ne 'dotnet' -or $entrypoint[1] -ne $ExpectedAssembly) {
        throw "Image '$Image' has unexpected entrypoint '$($entrypoint -join ' ')'."
    }

    if ($inspection.Config.Labels.'org.opencontainers.image.source' -ne $repositoryUrl) {
        throw "Image '$Image' does not identify the canonical Auth repository."
    }

    if ($inspection.Config.Labels.'org.opencontainers.image.revision' -ne $revision) {
        throw "Image '$Image' does not identify source revision '$revision'."
    }

    if ($inspection.Config.Labels.'org.opencontainers.image.version' -ne $buildVersion) {
        throw "Image '$Image' does not identify build version '$buildVersion'."
    }

    $configuredEnvironment = @($inspection.Config.Env) -join "`n"
    if ($configuredEnvironment -match 'GITHUB_PACKAGES_TOKEN') {
        throw "Image '$Image' retains the package-token environment variable."
    }

    $history = (& docker history --no-trunc --format '{{.CreatedBy}}' $Image) -join "`n"
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect image history for '$Image'."
    }

    if ($history.Contains($packageToken, [System.StringComparison]::Ordinal)) {
        throw "Image '$Image' history contains the package credential."
    }
}

function Remove-VerifiedImages {
    foreach ($image in $builtImages) {
        $inspection = Get-ImageInspection -Image $image
        if ($inspection.Config.Labels.'com.concertable.auth.image-verification' -ne $verificationId) {
            throw "Refusing to remove image '$image' because it is not owned by this verification run."
        }

        & docker image rm --force $image
        if ($LASTEXITCODE -ne 0) {
            throw "Could not remove verification image '$image'."
        }
    }
}

try {
    $commonArguments = @(
        'build',
        '--file', (Join-Path $repositoryRoot 'Dockerfile'),
        '--build-arg', "VCS_REF=$revision",
        '--build-arg', "BUILD_VERSION=$buildVersion",
        '--label', "com.concertable.auth.image-verification=$verificationId",
        '--pull'
    )

    Invoke-DockerBuildWithPackageToken `
        -Token $packageToken `
        -Arguments ($commonArguments + @(
            '--secret', 'id=github_packages_token,env=GITHUB_PACKAGES_TOKEN',
            '--target', 'auth-runtime',
            '--tag', $RuntimeImage,
            $repositoryRoot
        ))
    $builtImages.Add($RuntimeImage)

    & docker build `
        --file (Join-Path $repositoryRoot 'Dockerfile') `
        --build-arg "VCS_REF=$revision" `
        --build-arg "BUILD_VERSION=$buildVersion" `
        --label "com.concertable.auth.image-verification=$verificationId" `
        --pull `
        --target operational-store-migration `
        --tag $MigrationImage `
        $repositoryRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Migration image build failed with exit code $LASTEXITCODE."
    }
    $builtImages.Add($MigrationImage)

    Assert-ImageMetadata -Image $RuntimeImage -ExpectedAssembly 'Concertable.Auth.dll'
    Assert-ImageMetadata -Image $MigrationImage -ExpectedAssembly 'Concertable.Auth.OperationalStoreMigration.dll'
    $packageToken = $null

    $runtimeOutput = (& docker run --rm --entrypoint dotnet $RuntimeImage --list-runtimes) -join "`n"
    if ($LASTEXITCODE -ne 0 -or $runtimeOutput -notmatch 'Microsoft\.AspNetCore\.App 10\.') {
        throw 'Auth runtime image does not contain the expected ASP.NET Core 10 runtime.'
    }

    & docker run --rm --entrypoint /bin/sh $RuntimeImage -c 'test ! -e /app/appsettings.E2E.json'
    if ($LASTEXITCODE -ne 0) {
        throw 'Auth runtime image contains the E2E-only appsettings file.'
    }

    $migrationHelp = (& docker run --rm $MigrationImage --help) -join "`n"
    if ($LASTEXITCODE -ne 0 -or $migrationHelp -notmatch 'Copies Duende operational-store rows from B2BDb to AuthDb') {
        throw 'Operational-store migration image help smoke failed.'
    }

    Write-Host "Verified Auth images for revision ${revision}: $RuntimeImage, $MigrationImage."
}
finally {
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    $packageToken = $null
    if (-not $KeepImages -and $builtImages.Count -gt 0) {
        Remove-VerifiedImages
    }
}
