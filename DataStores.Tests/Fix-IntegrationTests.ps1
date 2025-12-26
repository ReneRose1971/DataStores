# PowerShell script to add PathProvider to Integration Tests
# This script adds "using TestHelper.DataStores.TestSetup;" and modifies service setup

$testFiles = @(
    "DataStores.Tests\Integration\BuilderPattern_Advanced_IntegrationTests.cs",
    "DataStores.Tests\Integration\BuilderPattern_EndToEnd_IntegrationTests.cs",
    "DataStores.Tests\Integration\BuilderPattern_Negative_IntegrationTests.cs"
)

foreach ($file in $testFiles) {
    $fullPath = Join-Path $PSScriptRoot $file
    
    if (Test-Path $fullPath) {
        $content = Get-Content $fullPath -Raw
        
        # Add using statement if not present
        if ($content -notmatch "using TestHelper\.DataStores\.TestSetup;") {
            $content = $content -replace "(using TestHelper\.DataStores\.PathProviders;)", "`$1`r`nusing TestHelper.DataStores.TestSetup;"
        }
        
        # Replace service creation pattern
        # Pattern: var services = new ServiceCollection();\n        new DataStoresServiceModule().Register(services);
        # Replace with: var services = DataStoreTestSetup.CreateTestServices();
        
        $pattern = 'var services = new ServiceCollection\(\);\s+new DataStoresServiceModule\(\)\.Register\(services\);'
        $replacement = 'var services = DataStoreTestSetup.CreateTestServices();'
        
        $newContent = $content -replace $pattern, $replacement
        
        if ($newContent -ne $content) {
            Set-Content -Path $fullPath -Value $newContent -NoNewline
            Write-Host "Updated: $file" -ForegroundColor Green
        } else {
            Write-Host "No changes needed: $file" -ForegroundColor Yellow
        }
    } else {
        Write-Host "File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`nDone!" -ForegroundColor Cyan
