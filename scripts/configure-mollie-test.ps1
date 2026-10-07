$ErrorActionPreference = 'Stop'
$taskApiProject = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../backend/src/MyShop.Api/MyShop.Api.csproj'))
$taskSecureKey = Read-Host 'Mollie test-API-sleutel (invoer verborgen)' -AsSecureString
$taskKey = [System.Net.NetworkCredential]::new('', $taskSecureKey).Password
if ($taskKey -cnotmatch '^test_[A-Za-z0-9]{20,64}$') { throw 'Gebruik uitsluitend een Mollie testsleutel die begint met test_.' }
$taskReturn = Read-Host 'Terugkeeradres (bijvoorbeeld http://localhost:4200/winkel/betaling)'
$taskWebhook = Read-Host 'Publiek HTTPS-meldingsadres (eindigend op /api/payments/mollie/webhook)'
$taskReturnUri = $null
$taskWebhookUri = $null
if (-not [Uri]::TryCreate($taskReturn, [UriKind]::Absolute, [ref]$taskReturnUri) -or
    $taskReturnUri.UserInfo -or $taskReturnUri.Query -or $taskReturnUri.Fragment -or
    ($taskReturnUri.Scheme -ne 'https' -and -not ($taskReturnUri.IsLoopback -and $taskReturnUri.Scheme -eq 'http'))) {
    throw 'Gebruik een HTTPS-terugkeeradres, of HTTP op localhost, zonder query of fragment.'
}
if (-not [Uri]::TryCreate($taskWebhook, [UriKind]::Absolute, [ref]$taskWebhookUri) -or
    $taskWebhookUri.Scheme -ne 'https' -or $taskWebhookUri.IsLoopback -or
    $taskWebhookUri.UserInfo -or $taskWebhookUri.Query -or $taskWebhookUri.Fragment -or
    $taskWebhookUri.AbsolutePath -ne '/api/payments/mollie/webhook') {
    throw 'Gebruik een publiek HTTPS-adres met het pad /api/payments/mollie/webhook, zonder query of fragment.'
}
try {
    # Pipe the JSON through standard input so the API key is absent from the command line and shell history.
    @{
        'Payments:Online:Provider' = 'Mollie'
        'Payments:Mollie:ApiKey' = $taskKey
        'Payments:Mollie:ReturnUrl' = $taskReturnUri.AbsoluteUri
        'Payments:Mollie:WebhookUrl' = $taskWebhookUri.AbsoluteUri
    } | ConvertTo-Json -Compress | dotnet user-secrets set --project $taskApiProject
    if ($LASTEXITCODE -ne 0) { throw 'Opslaan van lokale Mollie-instellingen is mislukt.' }
    Write-Host 'Mollie-testinstellingen zijn buiten de repository opgeslagen. Herstart de backend in Development en schakel online betalen in bij Betaalopties.'
} finally {
    $taskKey = $null
    $taskSecureKey.Dispose()
}
