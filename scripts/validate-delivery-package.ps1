[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Root
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Join-Path $PSScriptRoot '..'
}

$Root = (Resolve-Path -LiteralPath $Root).Path
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure {
    param([Parameter(Mandatory)][string]$Message)

    [void]$failures.Add($Message)
}

function Get-RequiredFile {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$Description
    )

    $path = Join-Path $Root ($RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Failure "Missing $Description`: $RelativePath"
        return $null
    }

    return $path
}

function Get-Text {
    param([Parameter(Mandatory)][string]$Path)

    return [IO.File]::ReadAllText($Path)
}

function Get-FrontMatterValue {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Key
    )

    $match = [regex]::Match($Text, "(?ms)^---\s*\r?\n.*?^${Key}:\s*(?<value>[^\r\n]+).*?^---\s*$")
    if (-not $match.Success) {
        return $null
    }

    $value = $match.Groups['value'].Value.Trim()
    return $value.Trim("'").Trim('"')
}

$agentMap = @(
    [pscustomobject]@{
        Name = 'delivery_milestone_supervisor'
        File = 'codex/agents/delivery-milestone-supervisor.toml'
        Skill = 'orchestrate-milestone-delivery'
    },
    [pscustomobject]@{
        Name = 'delivery_slice_planner'
        File = 'codex/agents/delivery-slice-planner.toml'
        Skill = 'plan-delivery-slices'
    },
    [pscustomobject]@{
        Name = 'delivery_slice_implementor'
        File = 'codex/agents/delivery-slice-implementor.toml'
        Skill = 'deliver-issue-slice'
    },
    [pscustomobject]@{
        Name = 'delivery_slice_tester'
        File = 'codex/agents/delivery-slice-tester.toml'
        Skill = 'author-slice-tests'
    },
    [pscustomobject]@{
        Name = 'delivery_slice_reviewer'
        File = 'codex/agents/delivery-slice-reviewer.toml'
        Skill = 'review-delivery-slice'
    }
)

$requiredSkills = @(
    [pscustomobject]@{ Name = 'orchestrate-milestone-delivery'; File = 'skills/delivery/orchestrate-milestone-delivery/SKILL.md' },
    [pscustomobject]@{ Name = 'plan-delivery-slices'; File = 'skills/delivery/plan-delivery-slices/SKILL.md' },
    [pscustomobject]@{ Name = 'deliver-issue-slice'; File = 'skills/delivery/deliver-issue-slice/SKILL.md' },
    [pscustomobject]@{ Name = 'delivery-runtime-protocol'; File = 'skills/delivery/delivery-runtime-protocol/SKILL.md' },
    [pscustomobject]@{ Name = 'author-slice-tests'; File = 'skills/delivery/author-slice-tests/SKILL.md' },
    [pscustomobject]@{ Name = 'review-delivery-slice'; File = 'skills/delivery/review-delivery-slice/SKILL.md' },
    [pscustomobject]@{ Name = 'development-session-observability'; File = 'skills/observability/development-session-observability/SKILL.md' },
    [pscustomobject]@{ Name = 'design-high-value-tests'; File = 'skills/testing/design-high-value-tests/SKILL.md' },
    [pscustomobject]@{ Name = 'verification-driven-delivery'; File = 'skills/testing/verification-driven-delivery/SKILL.md' }
)

$requiredSupportFiles = @(
    'skills/delivery/orchestrate-milestone-delivery/agents/openai.yaml',
    'skills/delivery/delivery-runtime-protocol/agents/openai.yaml',
    'skills/delivery/delivery-runtime-protocol/references/model-routing.md',
    'skills/delivery/delivery-runtime-protocol/references/review-artifacts.md',
    'skills/delivery/plan-delivery-slices/agents/openai.yaml',
    'skills/delivery/deliver-issue-slice/agents/openai.yaml',
    'skills/delivery/deliver-issue-slice/references/executable-contract.md',
    'skills/delivery/deliver-issue-slice/references/pull-request-review.md',
    'skills/delivery/deliver-issue-slice/references/rebase.md',
    'skills/delivery/author-slice-tests/agents/openai.yaml',
    'skills/delivery/author-slice-tests/references/green-baseline.md',
    'skills/delivery/author-slice-tests/references/green-finalization.md',
    'skills/delivery/author-slice-tests/references/rebase-conflict.md',
    'skills/delivery/author-slice-tests/references/red-contract.md',
    'skills/delivery/review-delivery-slice/agents/openai.yaml',
    'skills/delivery/review-delivery-slice/references/complete-change.md',
    'skills/delivery/review-delivery-slice/references/test-contract.md',
    'skills/observability/development-session-observability/agents/openai.yaml',
    'skills/observability/development-session-observability/references/event-schema.md',
    'skills/observability/development-session-observability/scripts/emit-marker.ps1',
    'skills/observability/development-session-observability/scripts/summarize-codex-sessions.ps1',
    'tests/AgentSkills.Tests/AgentSkills.Tests.csproj',
    'tests/AgentSkills.Tests/DevelopmentSessionObservabilityScriptTests.cs'
)

foreach ($relativePath in $requiredSupportFiles) {
    Get-RequiredFile -RelativePath $relativePath -Description 'required delivery support file' | Out-Null
}

foreach ($agent in $agentMap) {
    $path = Get-RequiredFile -RelativePath $agent.File -Description "custom agent profile"
    if ($null -eq $path) {
        continue
    }

    $text = Get-Text -Path $path
    foreach ($requiredKey in @('name', 'description', 'developer_instructions')) {
        if ($text -notmatch "(?m)^\s*$requiredKey\s*=") {
            Add-Failure "$($agent.File) must define '$requiredKey'."
        }
    }

    $nameMatch = [regex]::Match($text, '(?m)^\s*name\s*=\s*"(?<name>[^"]+)"')
    if ($nameMatch.Success -and $nameMatch.Groups['name'].Value -ne $agent.Name) {
        Add-Failure "$($agent.File) names '$($nameMatch.Groups['name'].Value)' but the package map expects '$($agent.Name)'."
    }

    $skillToken = '`$' + $agent.Skill
    if (-not $text.Contains($skillToken)) {
        Add-Failure "$($agent.File) must invoke '$skillToken'."
    }

    if ($text -match '(?im)^\s*(model|model_reasoning_effort)\s*=') {
        Add-Failure "$($agent.File) hard-codes a model or reasoning effort; routing belongs to the supervisor or explicit spawn request."
    }

    # The runtime may need to write build or test artifacts. The Reviewer role's
    # code-read-only boundary and ignored review-artifact exception are
    # the portable safety boundary.
}

$skillTexts = @{}
$localSkillNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($skill in $requiredSkills) {
    $path = Get-RequiredFile -RelativePath $skill.File -Description "required skill package"
    if ($null -eq $path) {
        continue
    }

    $text = Get-Text -Path $path
    $skillTexts[$skill.Name] = $text
    [void]$localSkillNames.Add($skill.Name)

    $frontMatterName = Get-FrontMatterValue -Text $text -Key 'name'
    if ([string]::IsNullOrWhiteSpace($frontMatterName)) {
        Add-Failure "$($skill.File) is missing a frontmatter name."
    }
    elseif ($frontMatterName -ne $skill.Name) {
        Add-Failure "$($skill.File) declares skill '$frontMatterName' but the package map expects '$($skill.Name)'."
    }
}

$readmePath = Get-RequiredFile -RelativePath 'README.md' -Description 'package README'
$readmeText = if ($null -ne $readmePath) { Get-Text -Path $readmePath } else { '' }

foreach ($agent in $agentMap) {
    $agentPath = [IO.Path]::GetFileName($agent.File)
    $skillToken = '`$' + $agent.Skill
    if (-not $readmeText.Contains($agent.Name) -or -not $readmeText.Contains($agentPath) -or -not $readmeText.Contains($skillToken)) {
        Add-Failure "README.md must index $($agent.Name), $agentPath, and $skillToken together."
    }
}

$externalDependencyPattern = '(?im)^\s*\|\s*`?\$[a-z][a-z0-9-]*`?\s*\|\s*external\s*\|.*Matt Pocock'
$externalDependencyLines = @([regex]::Matches($readmeText, $externalDependencyPattern) | ForEach-Object { $_.Value })

# Lower-case $tokens are skill references in delivery Markdown. Upper-case tokens
# are normally environment variables; the small allow-list covers placeholders
# used by prose and shell examples without weakening the dependency check.
$ignoredTokens = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($ignored in @('skill', 'path', 'root', 'home', 'codex_home', 'pwd', 'env', 'name', 'value', 'id', 'number', 'slug', 'branch', 'sha', 'commit', 'ref', 'url')) {
    [void]$ignoredTokens.Add($ignored)
}

foreach ($skill in $requiredSkills | Where-Object { $_.Name -in @('orchestrate-milestone-delivery', 'plan-delivery-slices', 'deliver-issue-slice', 'author-slice-tests', 'review-delivery-slice') }) {
    if (-not $skillTexts.ContainsKey($skill.Name)) {
        continue
    }

    $seenDependencies = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $matches = [regex]::Matches($skillTexts[$skill.Name], '\$(?<name>[a-z][a-z0-9-]*)')
    foreach ($match in $matches) {
        $dependency = $match.Groups['name'].Value
        if (-not $seenDependencies.Add($dependency)) {
            continue
        }

        if ($ignoredTokens.Contains($dependency)) {
            continue
        }

        if ($localSkillNames.Contains($dependency)) {
            continue
        }

        $dependencyToken = '`$' + $dependency + '`'
        $isDeclaredExternal = $externalDependencyLines | Where-Object { $_ -match [regex]::Escape($dependencyToken) }
        if ($null -eq $isDeclaredExternal -or @($isDeclaredExternal).Count -eq 0) {
            Add-Failure "Delivery skill '$($skill.Name)' references '$dependencyToken', which is neither a local skill nor an external Matt Pocock dependency declared in README.md."
        }
    }
}

$scanPaths = @()
$scanPaths += $agentMap | ForEach-Object { Join-Path $Root ($_.File -replace '/', [IO.Path]::DirectorySeparatorChar) }
$scanPaths += $requiredSkills | Where-Object { $_.Name -in @('orchestrate-milestone-delivery', 'delivery-runtime-protocol', 'plan-delivery-slices', 'deliver-issue-slice', 'author-slice-tests', 'review-delivery-slice', 'development-session-observability') } | ForEach-Object { Join-Path $Root ($_.File -replace '/', [IO.Path]::DirectorySeparatorChar) }

foreach ($path in $scanPaths | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }) {
    $relativePath = $path.Substring($Root.Length).TrimStart([IO.Path]::DirectorySeparatorChar, '/')
    $text = Get-Text -Path $path

    foreach ($forbidden in @(
        @{ Pattern = '(?i)ForTheLeague'; Reason = 'repository-specific name' }
        @{ Pattern = '(?i)\begil\b'; Reason = 'user-specific identity' }
        @{ Pattern = '(?i)EgilHansenEhf|EgilHansenEhf/\d+'; Reason = 'project or organization identifier' }
        @{ Pattern = '(?i)\b(?:always|must|only|should)\s+(?:use|run|spawn|select|create)\b.{0,60}\b(?:gpt-5\.6-luna|luna\s+max)\b'; Reason = 'fixed Luna model routing' }
        @{ Pattern = '(?i)\b(?:default|defaults|fixed)\s+(?:model|agent|routing)?\s*(?:is|to|:)\s*\b(?:gpt-5\.6-luna|luna\s+max)\b'; Reason = 'fixed Luna model routing' }
        @{ Pattern = '(?i)\b(?:gpt-5\.6-luna|luna\s+max)\b.{0,35}\b(?:as|for)\s+(?:the\s+)?(?:default|defaults|fixed)\b'; Reason = 'fixed Luna model routing' }
    )) {
        if ($text -match $forbidden.Pattern) {
            Add-Failure "$relativePath contains $($forbidden.Reason) ('$($forbidden.Pattern)')."
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Error ("Delivery package validation failed with {0} issue(s):`n- {1}" -f $failures.Count, ($failures -join "`n- "))
    exit 1
}

Write-Output ("Delivery package validation passed: {0} custom agents, {1} local skill packages, {2} support files, and declared dependency closure verified." -f $agentMap.Count, $requiredSkills.Count, $requiredSupportFiles.Count)
