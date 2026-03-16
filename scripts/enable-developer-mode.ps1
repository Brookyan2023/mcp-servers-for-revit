param(
    [int]$Minutes = 15,
    [string]$Note = "Temporary local unlock for Revit MCP developer mode"
)

if ($Minutes -le 0 -or $Minutes -gt 120) {
    throw "Minutes must be between 1 and 120."
}

$stateDir = Join-Path $HOME ".revit-mcp"
$stateFile = Join-Path $stateDir "developer-mode.json"

New-Item -ItemType Directory -Force -Path $stateDir | Out-Null

$now = [DateTime]::UtcNow
$expires = $now.AddMinutes($Minutes)

$state = [ordered]@{
    enabled = $true
    updatedAtUtc = $now.ToString("o")
    expiresAtUtc = $expires.ToString("o")
    note = $Note
}

$state | ConvertTo-Json | Set-Content -Path $stateFile -Encoding UTF8

Write-Output "Developer mode enabled until $($expires.ToString("u"))"
Write-Output "State file: $stateFile"
