# PowerShell Script to replace GetRelation with GetOneToManyRelation
$file = "DataStores.Tests\Unit\Relations\OneToOneRelationView_Tests.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'service\.GetRelation\(', 'service.GetOneToManyRelation('
Set-Content -Path $file -Value $content -NoNewline
Write-Host "Replaced GetRelation with GetOneToManyRelation in OneToOneRelationView_Tests.cs"
