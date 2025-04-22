# ==========================
# Remove-CyberHeader.ps1
# ==========================
Write-Host "🧽 Scrubbing injected cyberpunk headers..." -ForegroundColor Cyan

# Patterns to detect header lines
$headerLinePattern = '^// (\╔|\║|╚|═|▓|█|[\s]*)'
$maxHeaderLines = 25  # safety net: headers longer than this are suspicious

# Get all .cs files excluding bin/obj/.git
$files = Get-ChildItem -Path . -Include *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|.git)\\' }

foreach ($file in $files) {
    $filename = $file.Name
    $path = $file.FullName
    $lines = Get-Content $path

    # Look for contiguous lines that match the header pattern at the top of file
    $headerEndIndex = -1
    for ($i = 0; $i -lt [Math]::Min($lines.Length, $maxHeaderLines); $i++) {
        if ($lines[$i] -match $headerLinePattern) {
            $headerEndIndex = $i
        } else {
            break
        }
    }

    # If a header was found, remove it
    if ($headerEndIndex -ge 0) {
        Write-Host "🗑️  Removing header from: $filename" -ForegroundColor Yellow
        $remaining = $lines[($headerEndIndex + 1)..($lines.Length - 1)]
        Set-Content -Path $path -Value $remaining -Encoding UTF8
    } else {
        Write-Host "✔️  No header found in: $filename" -ForegroundColor DarkGray
    }
}

Write-Host "`n✅ All detectable headers have been removed (where present)." -ForegroundColor Green
