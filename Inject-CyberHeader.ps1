# ==========================
# Inject-CyberHeader.ps1
# ==========================

Write-Host "🔍 Initializing Git + GitVersion scan..." -ForegroundColor Cyan

# === Metadata ===
$gitVersionOutput = dotnet-gitversion /output json | Out-String | ConvertFrom-Json
$version = $gitVersionOutput.FullSemVer
$commitTimestamp = git log -1 --format="%cd" --date=iso
$author = git log -1 --format="%an"

# === Marker (used to detect already-injected files) ===
$marker = "// [CyberHeader] Injected by Hardline-Prophet"

# === File Scanner ===
$files = Get-ChildItem -Path . -Include *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|.git)\\' }

foreach ($file in $files) {
    $filename = $file.Name
    $path = $file.FullName

    # Check for injection marker
    if (Select-String -Path $path -Pattern ([regex]::Escape($marker)) -Quiet) {
        Write-Host "⏭️  Skipping already-injected file: $filename" -ForegroundColor DarkGray
        continue
    }

    Write-Host "💉 Injecting cyberpunk header into $filename" -ForegroundColor Green

    $header = @"
// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: $author
// ║ 🔢  GitVersion: $version
// ║ 📄  File: $filename
// ║ 🕒  Timestamp: $commitTimestamp
// $marker
"@

    $originalContent = Get-Content $path -Raw
    $newContent = $header + "`r`n" + $originalContent
    Set-Content -Path $path -Value $newContent -Encoding UTF8
}

Write-Host "`n✅ All applicable files have been injected." -ForegroundColor Cyan
