# Format-Fix.ps1
# 💉 Injects headers + 🧽 formats with CSharpier

Write-Host "`n🧼 Running dotnet-csharpier..." -ForegroundColor Cyan
dotnet-csharpier .

Write-Host "`n✅ Headers injected and code formatted." -ForegroundColor Green
