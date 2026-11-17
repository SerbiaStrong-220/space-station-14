#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-zA-Z0-9-]+$')]
    [string] $Login,

    [Parameter(Mandatory)]
    [string] $Path,

    [Parameter(Mandatory)]
    [string] $KnownContributors
)

$ErrorActionPreference = "Stop"

function Read-Contributors([string] $FilePath)
{
    if (-not (Test-Path -LiteralPath $FilePath))
    {
        return @()
    }

    $contents = [System.IO.File]::ReadAllText($FilePath).Trim()
    if ([string]::IsNullOrWhiteSpace($contents))
    {
        return @()
    }

    return @($contents -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}

if (-not (Test-Path -LiteralPath $KnownContributors -PathType Leaf))
{
    throw "The trusted contributors list is missing."
}

$known = @(Read-Contributors $KnownContributors)
if ($known | Where-Object { $_ -ieq $Login })
{
    Write-Host "$Login is already a contributor."
    exit 0
}

$contributors = @(Read-Contributors $Path)
if ($contributors | Where-Object { $_ -ieq $Login })
{
    Write-Host "$Login is already in the PR branch credits."
    exit 0
}

$contributors += $Login
$contents = (($contributors | Sort-Object -Unique) -join ", ") + "`n"
[System.IO.File]::WriteAllText($Path, $contents, [System.Text.UTF8Encoding]::new($false))

Write-Host "Added $Login to $Path."
