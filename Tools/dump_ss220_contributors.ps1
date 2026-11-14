#!/usr/bin/env pwsh

param(
    [string] $OutputPath = (Join-Path $PSScriptRoot "../Resources/Credits/SS220GitHub.txt")
)

$ErrorActionPreference = "Stop"

# Use merged PR authors, not /contributors: the latter also includes inherited upstream history.
$repository = "SerbiaStrong-220/space-station-14"
$headers = @{
    Accept = "application/vnd.github+json"
    "User-Agent" = "SS220-Credits"
}
if ($env:GITHUB_TOKEN)
{
    $headers.Authorization = "Bearer $env:GITHUB_TOKEN"
}

$authors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$mergedCount = 0
$page = 1
do
{
    $url = "https://api.github.com/repos/$repository/pulls?state=closed&base=master&sort=created&direction=asc&per_page=100&page=$page"
    $pullRequests = Invoke-RestMethod -Uri $url -Headers $headers

    foreach ($pr in $pullRequests)
    {
        if ($null -eq $pr.merged_at)
        {
            continue
        }

        $mergedCount++
        # IanComradeBot is an automation account registered as a regular GitHub user.
        if ($pr.user.type -eq "User" -and $pr.user.login -and $pr.user.login -ne "IanComradeBot")
        {
            [void] $authors.Add($pr.user.login)
        }
    }

    Write-Host "Page ${page}: $mergedCount merged PRs, $($authors.Count) authors."
    $page++
} while ($pullRequests.Count -eq 100)

if ($authors.Count -eq 0)
{
    throw "No merged PR authors found; refusing to overwrite the credits."
}

# Only replace the resource after all pages were successfully fetched.
$credits = ($authors | Sort-Object) -join ", "
[System.IO.File]::WriteAllText($OutputPath, "$credits`n", [System.Text.UTF8Encoding]::new($false))
