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

$lifted = @('Get-FindingLabels', 'Assert-NoSecrets', 'Assert-NoCriticalVulnerabilities')
foreach ($name in $lifted) {
    $definition = $ast.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    if ($definition.Count -eq 0) {
        throw "Could not find $name in verify-auth-release-candidate.ps1."
    }
    . ([scriptblock]::Create($definition[0].Extent.Text))
}

# Lifting functions out of a script silently drops any module-scope state they close over, and a harness
# missing a dependency reports a GATE defect while every clean case still passes for the wrong reason. So
# assert the lift is complete rather than assuming it: no lifted function may reference a variable it does
# not itself declare.
foreach ($name in $lifted) {
    $definition = $ast.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)[0]

    $declared = @($definition.Body.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
    $declared += @($definition.FindAll({
        param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst]
    }, $true) | ForEach-Object { $_.Left } |
        Where-Object { $_ -is [System.Management.Automation.Language.VariableExpressionAst] } |
        ForEach-Object { $_.VariablePath.UserPath })
    # A foreach variable is bound by the loop, not by an assignment, so a naive scan reports it as free and
    # cries wolf on every loop. Same for a trap/catch variable.
    $declared += @($definition.FindAll({
        param($node) $node -is [System.Management.Automation.Language.ForEachStatementAst]
    }, $true) | ForEach-Object { $_.Variable.VariablePath.UserPath })

    $free = @($definition.FindAll({
        param($node) $node -is [System.Management.Automation.Language.VariableExpressionAst]
    }, $true) | ForEach-Object { $_.VariablePath.UserPath } | Sort-Object -Unique |
        Where-Object { $_ -notin $declared -and $_ -notin @('_', 'null', 'true', 'false') })

    if ($free.Count -gt 0) {
        throw "$name closes over module-scope state this harness does not lift: $($free -join ', '). Lift it, or dot-source the script instead."
    }
}

$failures = 0

function Test-Gate {
    param(
        [Parameter(Mandatory)][string] $Label,
        [Parameter(Mandatory)][scriptblock] $Gate,
        [Parameter(Mandatory)][bool] $ExpectBlock,
        [Parameter(Mandatory)][int] $ExpectCount,
        [Parameter(Mandatory)][string] $ExpectedMessageLike
    )

    $blocked = $false
    $message = ''
    try { & $Gate }
    catch { $blocked = $true; $message = $_.Exception.Message }

    $correctReason = (-not $blocked) -or ($message -like $ExpectedMessageLike)

    # Assert the COUNT, not just that it blocked. Blocking is too coarse to separate the two opposite
    # malformed-report bugs: one counts a null and mangles the message, the other filters it and passes a
    # report whose only entry was that null. Both look correct under a blocks/does-not-block assertion
    # whenever a real finding happens to sit alongside.
    $reported = -1
    if ($blocked -and $message -match 'found (\d+) ') { $reported = [int] $Matches[1] }
    $countOk = (-not $blocked) -or ($reported -eq $ExpectCount)

    $passed = $correctReason -and $countOk -and ($blocked -eq $ExpectBlock)
    if (-not $passed) { $script:failures++ }

    $detail = ''
    if ($blocked -and -not $correctReason) { $detail = "  <-- threw for the wrong reason: $message" }
    elseif (-not $countOk) { $detail = "  <-- counted $reported, expected $ExpectCount" }
    Write-Host ('{0,-46} blocks={1,-6} count={2,-3} expected={3}/{4,-3} {5}{6}' -f
        $Label, $blocked, $(if ($reported -ge 0) { $reported } else { '-' }), $ExpectBlock, $ExpectCount,
        $(if ($passed) { 'PASS' } else { 'FAIL' }), $detail)
}

$secretLike = 'Secret scan found*'
Test-Gate 'secrets: Results absent'              { Assert-NoSecrets -Report ([pscustomobject]@{ SchemaVersion = 2 }) -Subject 't' } $false 0 $secretLike
Test-Gate 'secrets: Results null'                { Assert-NoSecrets -Report ([pscustomobject]@{ Results = $null }) -Subject 't' } $false 0 $secretLike
Test-Gate 'secrets: Secrets absent'              { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'a' }) }) -Subject 't' } $false 0 $secretLike
Test-Gate 'secrets: Secrets empty'               { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'a'; Secrets = @() }) }) -Subject 't' } $false 0 $secretLike
Test-Gate 'secrets: one finding'                 { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'a'; Secrets = @([pscustomobject]@{ RuleID = 'aws-access-key-id' }) }) }) -Subject 't' } $true 1 $secretLike
Test-Gate 'secrets: two findings, two results'   { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @(
        [pscustomobject]@{ Target = 'a'; Secrets = @([pscustomobject]@{ RuleID = 'r1' }) },
        [pscustomobject]@{ Target = 'b'; Secrets = @([pscustomobject]@{ RuleID = 'r2' }) }) }) -Subject 't' } $true 2 $secretLike

# A null ELEMENT is not the same shape as a null Results, and the second case is the one that matters:
# a real credential sitting beside a null must still be REPORTED AS A CREDENTIAL. A gate that dies on the
# null fails closed, but whoever triages it sees a broken script rather than a secret.
Test-Gate 'secrets: [null] element only'         { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @($null) }) -Subject 't' } $false 0 $secretLike
Test-Gate 'secrets: [null, real finding]'        { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @(
        $null,
        [pscustomobject]@{ Target = 'b'; Secrets = @([pscustomobject]@{ RuleID = 'r2' }) }) }) -Subject 't' } $true 1 $secretLike
# Deliberate asymmetry: nulls are FILTERED at the container level but COUNTED at the finding level.
# Blocking on a malformed report is the safe direction; filtering there would pass it clean.
Test-Gate 'secrets: null INSIDE Secrets'         { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @(
        [pscustomobject]@{ Target = 'a'; Secrets = @($null) }) }) -Subject 't' } $true 1 $secretLike
Test-Gate 'secrets: null + real INSIDE Secrets'  { Assert-NoSecrets -Report ([pscustomobject]@{ Results = @(
        [pscustomobject]@{ Target = 'a'; Secrets = @($null, [pscustomobject]@{ RuleID = 'r3' }) }) }) -Subject 't' } $true 2 $secretLike

$vulnLike = 'Vulnerability scan found*'
Test-Gate 'vulns: Results absent'                { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ SchemaVersion = 2 }) -Subject 't' } $false 0 $vulnLike
Test-Gate 'vulns: Results null'                  { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = $null }) -Subject 't' } $false 0 $vulnLike
Test-Gate 'vulns: Vulnerabilities absent'        { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'img' }) }) -Subject 't' } $false 0 $vulnLike
Test-Gate 'vulns: Vulnerabilities empty'         { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'img'; Vulnerabilities = @() }) }) -Subject 't' } $false 0 $vulnLike
Test-Gate 'vulns: one CRITICAL'                  { Assert-NoCriticalVulnerabilities -Report ([pscustomobject]@{ Results = @([pscustomobject]@{ Target = 'img'; Vulnerabilities = @([pscustomobject]@{ VulnerabilityID = 'CVE-2026-0001' }) }) }) -Subject 't' } $true 1 $vulnLike

if ($failures -gt 0) {
    throw "$failures Trivy gate case(s) failed."
}

Write-Host 'All Trivy gate cases passed: clean reports pass, findings block, and each blocks for its own reason.'
