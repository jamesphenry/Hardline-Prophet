# ==========================
# Update-CyberHeaderVersion.ps1
# ==========================

Write-Host "🧬 Updating GitVersion in cyberpunk headers..." -ForegroundColor Cyan

# Get the current version from GitVersion
$gitVersionOutput = dotnet-gitversion /output json | Out-String | ConvertFrom-Json
$currentVersion = $gitVersionOutput.FullSemVer

# Regex to match the GitVersion line
$versionLinePattern = '^// \║ 🔢  GitVersion: .+$'
$versionLineReplacement = "// ║ 🔢  GitVersion: $currentVersion"

# Get all .cs files except bin/obj/.git
$files = Get-ChildItem -Path . -Include *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|.git)\\' }

foreach ($file in $files) {
    $content = Get-Content $file.FullName

    # Check if it contains a GitVersion header line
    $hasVersionLine = $content | Where-Object { $_ -match $versionLinePattern }
    if ($hasVersionLine) {
        Write-Host "🔄 Updating version in: $($file.Name)" -ForegroundColor Yellow

        $updatedContent = $content -replace $versionLinePattern, $versionLineReplacement
        Set-Content -Path $file.FullName -Value $updatedContent -Encoding UTF8
    } else {
        Write-Host "✔️  No version header found in: $($file.Name)" -ForegroundColor DarkGray
    }
}

Write-Host "`n✅ All matching headers updated to GitVersion: $currentVersion" -ForegroundColor Green
