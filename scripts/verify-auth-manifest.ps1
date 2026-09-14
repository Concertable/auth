[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'concertable-auth-manifest.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$appHostProject = Join-Path $repositoryRoot 'local/AppHost/Concertable.Auth.AppHost.csproj'
$resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath, $repositoryRoot)
$outputDirectory = Split-Path -Parent $resolvedOutputPath

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

& dotnet run `
    --project $appHostProject `
    --configuration $Configuration `
    --no-build `
    -- `
    --publisher manifest `
    --output-path $resolvedOutputPath

if ($LASTEXITCODE -ne 0) {
    throw "Aspire manifest publication failed with exit code $LASTEXITCODE."
}

$manifestJson = Get-Content -LiteralPath $resolvedOutputPath -Raw
$manifest = $manifestJson | ConvertFrom-Json
$resourceNames = @($manifest.resources.PSObject.Properties.Name)

if ($resourceNames -notcontains 'AuthDb') {
    throw "The Auth AppHost manifest does not contain the required AuthDb resource."
}

if ($manifestJson -match '(?i)B2BDb') {
    throw "The Auth AppHost manifest still contains the forbidden B2BDb dependency."
}

Write-Output "Verified Auth AppHost manifest at $resolvedOutputPath (AuthDb present; B2BDb absent)."
