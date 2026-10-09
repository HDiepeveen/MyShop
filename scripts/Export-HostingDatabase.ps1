param([Parameter(Mandatory=$true)][string]$PackageDirectory,
      [string]$SourceConnection = 'Server=(localdb)\MSSQLLocalDB;Database=MyShopPaymentAcceptance_20261007;Integrated Security=True;TrustServerCertificate=True')
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
$taskOutput = [IO.Path]::GetFullPath($PackageDirectory)
if (!(Test-Path -LiteralPath $taskOutput)) { throw 'Hostingpakketmap bestaat niet.' }
if ($taskOutput.Equals($taskRepo, [StringComparison]::OrdinalIgnoreCase) -or $taskOutput.StartsWith($taskRepo + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Bewaar de export buiten Git.' }
$taskSchema = Join-Path $taskOutput 'database-schema.sql'
$taskData = Join-Path $taskOutput 'database-data.sql'
if ((Test-Path -LiteralPath $taskSchema) -or (Test-Path -LiteralPath $taskData)) { throw 'Bestaande exports worden niet overschreven.' }
dotnet ef migrations script --no-build --project (Join-Path $taskRepo 'backend/src/MyShop.Infrastructure') --startup-project (Join-Path $taskRepo 'backend/src/MyShop.Infrastructure') --output $taskSchema
if ($LASTEXITCODE -ne 0) { throw 'Schemascript maken mislukt.' }
# Compile statements after ADD COLUMN in a fresh batch: SQL Server cannot see
# a newly added column while compiling the rest of the original batch.
# GO preserves the migration transaction because the installer uses one connection.
$taskSchemaText = [IO.File]::ReadAllText($taskSchema)
$taskSchemaText = [Regex]::Replace($taskSchemaText,
    '(?m)^(ALTER TABLE \[[^\r\n]+\] ADD \[[^\r\n]+;)\r?$', '$1' + "`r`nGO")
[IO.File]::WriteAllText($taskSchema, $taskSchemaText, [Text.UTF8Encoding]::new($false))
function Quote-Identifier([string]$value) { '[' + $value.Replace(']', ']]') + ']' }
function Sql-Literal($value) {
    if ($value -is [DBNull]) { return 'NULL' }
    if ($value -is [byte[]]) { return '0x' + [BitConverter]::ToString($value).Replace('-', '') }
    if ($value -is [bool]) { if ($value) { return '1' } else { return '0' } }
    if ($value -is [DateTime]) { return "'" + $value.ToString('yyyy-MM-ddTHH:mm:ss.fffffff', [Globalization.CultureInfo]::InvariantCulture) + "'" }
    if ($value -is [DateTimeOffset]) { return "'" + $value.ToString('O', [Globalization.CultureInfo]::InvariantCulture) + "'" }
    if ($value -is [string] -or $value -is [Guid] -or $value -is [TimeSpan]) { return "N'" + $value.ToString().Replace("'", "''") + "'" }
    if ($value -is [IFormattable]) { return $value.ToString($null, [Globalization.CultureInfo]::InvariantCulture) }
    throw "Onbekend gegevenstype: $($value.GetType().FullName)"
}
$taskConnection = New-Object System.Data.SqlClient.SqlConnection($SourceConnection)
$taskWriter = $null
$taskTransaction = $null
try {
    $taskConnection.Open()
    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.Transaction = $taskTransaction
    $taskCommand.CommandText = "SELECT SCHEMA_NAME(schema_id) AS SchemaName,name FROM sys.tables WHERE is_ms_shipped=0 AND name <> '__EFMigrationsHistory' ORDER BY name"
    $taskTables = New-Object System.Data.DataTable
    $taskTables.Load($taskCommand.ExecuteReader())
    $taskWriter = New-Object IO.StreamWriter($taskData, $false, [Text.UTF8Encoding]::new($false))
    $taskWriter.WriteLine("SET XACT_ABORT ON; BEGIN TRY BEGIN TRANSACTION;")
    $taskWriter.WriteLine("IF DB_NAME() <> @ExpectedDatabaseName THROW 50000, 'Unexpected target database.', 1;")
    $taskWriter.WriteLine("IF EXISTS (SELECT 1 FROM Products) OR EXISTS (SELECT 1 FROM AspNetUsers) OR EXISTS (SELECT 1 FROM Orders) THROW 50001, 'Target contains shop data; import refused.', 1;")
    foreach ($taskTable in $taskTables.Rows) {
        $taskQualified = (Quote-Identifier $taskTable.SchemaName) + '.' + (Quote-Identifier $taskTable.name)
        $taskWriter.WriteLine("ALTER TABLE $taskQualified NOCHECK CONSTRAINT ALL;")
    }
    foreach ($taskTable in $taskTables.Rows) {
        $taskQualified = (Quote-Identifier $taskTable.SchemaName) + '.' + (Quote-Identifier $taskTable.name)
        $taskWriter.WriteLine("DELETE FROM $taskQualified;")
    }
    foreach ($taskTable in $taskTables.Rows) {
        $taskQualified = (Quote-Identifier $taskTable.SchemaName) + '.' + (Quote-Identifier $taskTable.name)
        $taskCommand.CommandText = "SELECT name,is_identity FROM sys.columns WHERE object_id=OBJECT_ID(N'$taskQualified') AND is_computed=0 ORDER BY column_id"
        $taskColumns = New-Object System.Data.DataTable
        $taskColumns.Load($taskCommand.ExecuteReader())
        $taskNames = @($taskColumns.Rows | ForEach-Object { Quote-Identifier $_.name }) -join ','
        $taskIdentity = @($taskColumns.Rows | Where-Object { $_.is_identity }).Count -gt 0
        if ($taskIdentity) { $taskWriter.WriteLine("SET IDENTITY_INSERT $taskQualified ON;") }
        $taskCommand.CommandText = "SELECT $taskNames FROM $taskQualified"
        $taskReader = $taskCommand.ExecuteReader()
        try {
            while ($taskReader.Read()) {
                $taskValues = for ($taskColumn=0; $taskColumn -lt $taskReader.FieldCount; $taskColumn++) { Sql-Literal ($taskReader.GetValue($taskColumn)) }
                $taskWriter.WriteLine("INSERT INTO $taskQualified ($taskNames) VALUES ($($taskValues -join ','));")
            }
        } finally { $taskReader.Dispose() }
        if ($taskIdentity) { $taskWriter.WriteLine("SET IDENTITY_INSERT $taskQualified OFF;") }
    }
    $taskWriter.WriteLine("UPDATE EmailSettings SET Enabled=0,ProtectedPassword=NULL,PublicBaseUrl=@PublicBaseUrl,Version=NEWID() WHERE Id='13c5dc48-ce9a-44ef-9fb4-5c6d369cfeb6'; IF @@ROWCOUNT <> 1 THROW 50002, 'Email settings missing.', 1;")
    foreach ($taskTable in $taskTables.Rows) {
        $taskQualified = (Quote-Identifier $taskTable.SchemaName) + '.' + (Quote-Identifier $taskTable.name)
        $taskWriter.WriteLine("ALTER TABLE $taskQualified WITH CHECK CHECK CONSTRAINT ALL;")
    }
    $taskWriter.WriteLine("COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;")
    $taskTransaction.Commit()
    Write-Host 'Structuur en gegevens zijn geexporteerd. Deze bestanden bevatten accounts en instellingen; upload ze nooit in de websitemap.'
} finally {
    if ($taskWriter) { $taskWriter.Dispose() }
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
