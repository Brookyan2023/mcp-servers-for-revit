$stateFile = Join-Path (Join-Path $HOME ".revit-mcp") "developer-mode.json"

if (Test-Path $stateFile) {
    Remove-Item $stateFile -Force
    Write-Output "Developer mode disabled."
    Write-Output "Removed state file: $stateFile"
} else {
    Write-Output "Developer mode was already disabled."
    Write-Output "State file not found: $stateFile"
}
