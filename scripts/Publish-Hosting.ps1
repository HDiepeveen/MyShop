param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
$taskOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $taskOutput) { throw 'Kies een nieuwe uitvoermap; bestaande pakketten worden niet overschreven.' }
if ($taskOutput.Equals($taskRepo, [StringComparison]::OrdinalIgnoreCase) -or $taskOutput.StartsWith($taskRepo + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Kies een uitvoermap buiten de repository.'
}
New-Item -ItemType Directory -Path $taskOutput | Out-Null
Push-Location (Join-Path $taskRepo 'frontend')
try {
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontendbuild mislukt.' }
} finally { Pop-Location }
$taskPublish = Join-Path $taskOutput 'website'
dotnet publish (Join-Path $taskRepo 'backend/src/MyShop.Api') -c Release --no-restore -p:UseAppHost=false -o $taskPublish
if ($LASTEXITCODE -ne 0) { throw 'Publiceren mislukt.' }
$taskWebRoot = Join-Path $taskPublish 'wwwroot'
New-Item -ItemType Directory -Force -Path $taskWebRoot | Out-Null
Copy-Item -Path (Join-Path $taskRepo 'frontend/dist/myshop-admin/browser/*') -Destination $taskWebRoot -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Configure-Hosting.ps1') -Destination $taskOutput
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-HostingDatabase.ps1') -Destination $taskOutput
Write-Host "Hostingbestanden staan in $taskOutput. Voer Configure-Hosting.ps1 uit om het databasewachtwoord lokaal in te stellen en de uploadzip te maken."
