[CmdletBinding()]
param(
    [string] $RunId, [string] $WorkItem,
    [ValidateSet('supervisor', 'planner', 'implementor', 'tester', 'reviewer')][string] $Role,
    [ValidateSet('discovery', 'planning', 'implementation', 'meaningful-red', 'green-refactor', 'debugging', 'verification', 'review-remediation', 'publication', 'tool-wait', 'external-wait', 'human-wait')][string] $Phase,
    [ValidateSet('succeeded', 'failed', 'blocked', 'incomplete', 'not-run')][string] $Result,
    [switch] $WorkCycle,
    [ValidateSet('passed', 'failed', 'not-run', 'not-applicable', 'unknown')][string] $QualityOutcome,
    [Nullable[long]] $FindingCount, [Nullable[long]] $ResolvedFindingCount,
    [ValidateSet('dependency', 'contention', 'environment', 'external-service', 'human-decision', 'review', 'unknown')][string] $Blocker,
    [ValidateSet('succeeded', 'failed', 'blocked', 'cancelled', 'incomplete')][string] $Outcome,
    [ValidateSet('routine', 'complexity', 'risk', 'escalation', 'de-escalation')][string] $RoutingClass,
    [string] $RoutingRationale
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
foreach ($count in @($FindingCount, $ResolvedFindingCount)) { if ($null -ne $count -and $count -lt 0) { throw 'Finding counts must be non-negative.' } }
if (-not [string]::IsNullOrWhiteSpace($RoutingRationale) -and ($RoutingRationale.Length -gt 240 -or $RoutingRationale -match '[\r\n]')) { throw 'RoutingRationale must be a short single line.' }
if (($null -ne $FindingCount -or $null -ne $ResolvedFindingCount) -and [string]::IsNullOrWhiteSpace($QualityOutcome)) { throw 'QualityOutcome is required with finding counts.' }
if ((-not [string]::IsNullOrWhiteSpace($RunId) -or -not [string]::IsNullOrWhiteSpace($WorkItem) -or -not [string]::IsNullOrWhiteSpace($Role)) -and ([string]::IsNullOrWhiteSpace($RunId) -or [string]::IsNullOrWhiteSpace($WorkItem) -or [string]::IsNullOrWhiteSpace($Role))) { throw 'RunId, WorkItem, and Role must be supplied together.' }
if ([string]::IsNullOrWhiteSpace($Phase) -and [string]::IsNullOrWhiteSpace($Result) -and -not $WorkCycle -and [string]::IsNullOrWhiteSpace($QualityOutcome) -and [string]::IsNullOrWhiteSpace($Blocker) -and [string]::IsNullOrWhiteSpace($Outcome) -and [string]::IsNullOrWhiteSpace($RoutingClass)) { throw 'Supply at least one semantic field.' }
$marker = [ordered]@{ schemaVersion = 1 }
foreach ($pair in @{ runId=$RunId; workItem=$WorkItem; role=$Role; phase=$Phase; result=$Result; qualityOutcome=$QualityOutcome; blocker=$Blocker; outcome=$Outcome; routingClass=$RoutingClass; routingRationale=$RoutingRationale }.GetEnumerator()) { if (-not [string]::IsNullOrWhiteSpace($pair.Value)) { $marker[$pair.Key] = $pair.Value } }
if ($WorkCycle) { $marker.workCycle = $true }; if ($null -ne $FindingCount) { $marker.findingCount = $FindingCount }; if ($null -ne $ResolvedFindingCount) { $marker.resolvedFindingCount = $ResolvedFindingCount }
Write-Output ('CODEX_DELIVERY_MARKER:' + ($marker | ConvertTo-Json -Compress))
