<# Bootstrap this service's standalone AppHost. Foreign runtimes remain pinned containers. #>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'tools/OwnerOperations.psm1') -Force
Initialize-OwnerDevelopment -Root $PSScriptRoot `
    -AppHostProject 'local/AppHost/Concertable.Auth.AppHost.csproj' `
    -SettingsProjects @('api/src/Concertable.Auth') `
    -SecretKeys @('ServiceAuth:B2BClientSecret', 'ServiceAuth:CustomerClientSecret', 'ServiceAuth:AuthClientSecret') `
    -WhatIf:$WhatIfPreference
Write-Host "Run: dotnet run --project '$PSScriptRoot/local/AppHost'"
