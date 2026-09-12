<#
    Exercises verify-auth-release-candidate.ps1's Trivy gate helpers against the report shapes Trivy
    actually emits, without running a scan — a real scan costs 20-30 minutes under load, which is far too
    slow a feedback loop for the failure mode these helpers exist to survive.

    The helpers are lifted out of the release-candidate script through the PowerShell AST rather than
    copied, so this tests the shipped code and cannot drift from it.

    What it is guarding: on a CLEAN scan Trivy omits Results/Secrets/Vulnerabilities entirely rather than
    emitting them empty, and Results can also be JSON null. Under Set-StrictMode -Version Latest every one
    of those throws on property access, so the gate would fail on exactly the runs that should pass — the
    failure path is the normal path, which is why it survives casual testing.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'verify-auth-release-candidate.ps1'
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$null, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) {
    throw "verify-auth-release-candidate.ps1 does not parse: $($parseErrors[0].Message)"
}

foreach ($name in @('Assert-NoSecrets', 'Assert-NoCriticalVulnerabilities')) {
    $definition = $ast.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    if ($definition.Count -eq 0) {
        throw "Could not find $name in verify-auth-release-candidate.ps1."
    }
    . ([scriptblock]::Create($definition[0].Extent.Text))
}

$failures = 0

function Test-Gate {
    param(
        [Parameter(Mandatory)][string] $Label,
        [Parameter(Mandatory)][scriptblock] $Gate,
        [Parameter(Mandatory)][bool] $ExpectBlock,
        [Parameter(Mandatory)][string] $ExpectedMessageLike
    )

    $blocked = $false
    $message = ''
    try { & $Gate }
    catch { $blocked = $true; $message = $_.Exception.Message }

    $correctReason = (-not $blocked) -or ($message -like $ExpectedMessageLike)
    $passed = $correctReason -and ($blocked -eq $ExpectBlock)
    if (-not $passed) { $script:failures++ }

    $detail = if ($blocked -and -not $correctReason) { "  <-- threw for the wrong reason: $message" } else { '' }
    Write-Host ('{0,-46} blocks={1,-6} expected={2,-6} {3}{4}' -f
        $Label, $blocked, $ExpectBlock, $(if ($passed) { 'PASS' } else { 'FAIL' }), $detail)
}

$secretLike = 'Secret scan found*'
Test-Gate 'secrets: Results absent'              { Assert-NoSecrets -Report ([pscustomobject]@{ SchemaVersion = 2 }) -Subject 't' } $false $secretLike
Test-Gate 'secrets: Results null'                { Assert-NoSecrets -Report ([pscustomobject]@{ Results = $null }) -Subject 't' } $false $secretLike
Test-Gate 'secrets: Secrets absent'              { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'a' }) }) -Subject 't' } $false $secretLike
Test-Gate 'secrets: Secrets empty'               { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'a'; Secrets = @() }) }) -Subject 't' } $false $secretLike
Test-Gate 'secrets: one finding'                 { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'a'; Secrets = @([pscustomobject]@{ RuleID = 'aws-access-key-id' }) }) }) -Subject 't' } $true $secretLike
Test-Gate 'secrets: two findings, two results'   { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @(
        [pscustomobject]@{ Target = 'a'; Secrets = @([pscustomobject]@{ RuleID = 'r1' }) },
        [pscustomobject]@{ Target = 'b'; Secrets = @([pscustomobject]@{ RuleID = 'r2' }) }) }) -Subject 't' } $true $secretLike

$vulnLike = 'Vulnerability scan found*'
Test-Gate 'vulns: Results absent'                { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ SchemaVersion = 2 }) -Subject 't' } $false $vulnLike
Test-Gate 'vulns: Results null'                  { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = $null }) -Subject 't' } $false $vulnLike
Test-Gate 'vulns: Vulnerabilities absent'        { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'img' }) }) -Subject 't' } $false $vulnLike
Test-Gate 'vulns: Vulnerabilities empty'         { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'img'; Vulnerabilities = @() }) }) -Subject 't' } $false $vulnLike
Test-Gate 'vulns: one CRITICAL'                  { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'img'; Vulnerabilities = @([pscustomobject]@{ VulnerabilityID = 'CVE-2026-0001' }) }) }) -Subject 't' } $true $vulnLike

if ($failures -gt 0) {
    throw "$failures Trivy gate case(s) failed."
}

Write-Host 'All Trivy gate cases passed: clean reports pass, findings block, and each blocks for its own reason.'
