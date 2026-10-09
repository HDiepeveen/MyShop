param(
    [string]$WebsiteAddress = 'https://hansdiepeveen-001-site1.ktempurl.com',
    [string]$DatabaseServer = 'sql6033.site4now.net',
    [string]$DatabaseName = 'db_acf835_myshop',
    [string]$DatabaseUser = 'db_acf835_myshop_admin'
)
$ErrorActionPreference = 'Stop'
$taskWebsite = Join-Path $PSScriptRoot 'website'
if (!(Test-Path -LiteralPath (Join-Path $taskWebsite 'MyShop.Api.dll'))) { throw 'Dit script hoort naast de map website uit het hostingpakket.' }
$taskUri = [Uri]$WebsiteAddress
if ($taskUri.Scheme -ne 'https' -or !$taskUri.IsAbsoluteUri) { throw 'Gebruik een HTTPS-websiteadres.' }
$taskSecret = Read-Host 'Databasewachtwoord (wordt niet getoond)' -AsSecureString
if ($taskSecret.Length -eq 0) { throw 'Een databasewachtwoord is vereist.' }
try {
    $taskConnection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $taskConnection["Data Source"] = $DatabaseServer
    $taskConnection["Initial Catalog"] = $DatabaseName
    $taskConnection["User ID"] = $DatabaseUser
    $taskConnection["Password"] = (New-Object System.Net.NetworkCredential('', $taskSecret)).Password
    $taskConnection["Encrypt"] = $true
    $taskConnection["TrustServerCertificate"] = $false
    $taskSettings = @{
        ConnectionStrings = @{ MyShop = $taskConnection.ConnectionString }
        AllowedHosts = $taskUri.Host
        Hosting = @{ DataProtectionKeyPath = 'App_Data/keys' }
        Email = @{ Enabled = $false; Mode = 'Smtp'; PublicBaseUrl = $taskUri.GetLeftPart([UriPartial]::Authority) }
        Payments = @{ Online = @{ Provider = '' } }
        Logging = @{ LogLevel = @{ Default = 'Information'; 'Microsoft.AspNetCore' = 'Warning' } }
    }
    [System.IO.File]::WriteAllText((Join-Path $taskWebsite 'appsettings.Production.json'), ($taskSettings | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))
    $taskZip = Join-Path $PSScriptRoot 'MyShop-upload.zip'
    Compress-Archive -Path (Join-Path $taskWebsite '*') -DestinationPath $taskZip -Force
    Write-Host "Uploadpakket gemaakt: $taskZip. Dit pakket bevat het databasewachtwoord; bewaar het prive en upload de databasebackup nooit naar /site1."
} finally {
    if ($taskConnection) { $taskConnection["Password"] = ''; $taskConnection.Clear() }
    $taskSettings = $null
    $taskSecret.Dispose()
}
