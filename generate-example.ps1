$content = Get-Content "appsettings.json" -Raw
$updated = $content -replace '"ApiKey":\s*"[^"]*"', '"ApiKey": "TU_API_KEY_AQUI"'
Set-Content -Path "appsettings.example.json" -Value $updated
Write-Host "appsettings.example.json generado correctamente"