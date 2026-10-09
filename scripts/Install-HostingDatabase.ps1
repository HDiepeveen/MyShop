$ErrorActionPreference = 'Stop'
$taskConfig = Join-Path $PSScriptRoot 'website/appsettings.Production.json'
if (!(Test-Path -LiteralPath $taskConfig)) { throw 'Voer eerst Configure-Hosting.ps1 uit.' }
$taskSettings = Get-Content -LiteralPath $taskConfig -Raw | ConvertFrom-Json
$taskConnection = New-Object System.Data.SqlClient.SqlConnection($taskSettings.ConnectionStrings.MyShop)
try {
    $taskConnection.Open()
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.CommandText = 'SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0'
    if ([int]$taskCommand.ExecuteScalar() -ne 0) { throw 'De doel-database is niet leeg. Er is niets gewijzigd.' }
    $taskSchema = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'database-schema.sql'))
    $taskBatchIndex = 0
    foreach ($taskBatch in [Regex]::Split($taskSchema, '(?im)^\s*GO\s*\r?$')) {
        if ([string]::IsNullOrWhiteSpace($taskBatch)) { continue }
        $taskBatchIndex++
        $taskStage = "schema batch $taskBatchIndex"
        $taskCommand.CommandText = $taskBatch
        $taskCommand.CommandTimeout = 300
        [void]$taskCommand.ExecuteNonQuery()
    }
    $taskStage = "data"
    $taskCommand.CommandText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'database-data.sql'))
    [void]$taskCommand.Parameters.Add('@ExpectedDatabaseName', [System.Data.SqlDbType]::NVarChar, 128)
    $taskCommand.Parameters['@ExpectedDatabaseName'].Value = $taskConnection.Database
    [void]$taskCommand.Parameters.Add('@PublicBaseUrl', [System.Data.SqlDbType]::NVarChar, 2000)
    $taskCommand.Parameters['@PublicBaseUrl'].Value = $taskSettings.Email.PublicBaseUrl
    [void]$taskCommand.ExecuteNonQuery()
    Write-Host 'Database overgezet. Stel na de upload de Brevo-sleutel opnieuw in bij E-mailinstellingen en stuur een testmail.'
} catch {
    throw ("Database-installatie niet voltooid ($taskStage): " + $_.Exception.Message)
} finally { $taskConnection.Dispose(); $taskSettings = $null }
