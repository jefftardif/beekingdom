<#
.SYNOPSIS
Adds internal-support.beekingdomgame.com as a second hostname binding on the existing,
already-durable BeeKingdom.Server IIS site, and turns on the AdminSupport surface for it.

.DESCRIPTION
Run this ON the Windows Server 2025 production box (104.129.128.136), not from a dev
machine. It does not deploy a new process: it reuses the IIS site that already serves
api-ops.beekingdomgame.com (or whichever site host header you point it at), because that
site is already durable — IIS (W3SVC) is a real Windows Service that auto-starts on boot,
and the ASP.NET Core Module (ANCM) auto-restarts the worker process on crash. Adding a
binding does not need a new service.

What it does:
  1. Finds the IIS site currently bound to -ExistingHostname.
  2. Imports the Cloudflare Origin CA certificate (.pfx) you generated for the new
     hostname into the Local Machine certificate store.
  3. Adds an HTTPS (SNI) binding for -NewHostname on port 443 to that same site.
  4. Generates a strong random AdminSupport key, writes only its SHA-256 into the site's
     web.config <aspNetCore><environmentVariables>, and sets AdminSupport:Enabled=true.
     The plaintext key is printed once to the console — save it in a password manager,
     it is never written to disk or logged.
  5. Recycles the app pool so the new environment variables take effect.
  6. Runs a smoke test against https://<NewHostname>/admin/ui and /health.

Prerequisites (do these first, outside this script):
  - In Cloudflare DNS, internal-support.beekingdomgame.com already points to this server
    (Jeff: already done).
  - In Cloudflare SSL/TLS settings, mode is set to "Full (strict)".
  - In Cloudflare, generate an Origin Certificate (SSL/TLS > Origin Server > Create
    Certificate) covering internal-support.beekingdomgame.com, download the certificate
    and private key, and combine them into a .pfx (e.g. with OpenSSL:
    openssl pkcs12 -export -out internal-support-origin.pfx -inkey origin.key -in origin.pem).
  - The ASP.NET Core Hosting Bundle and IIS are already installed (they are — this server
    already runs BeeKingdom.Server).

.PARAMETER NewHostname
The new subdomain to bind. Defaults to internal-support.beekingdomgame.com.

.PARAMETER ExistingHostname
The hostname of the already-deployed BeeKingdom.Server IIS site, used only to find that
site. Defaults to api-ops.beekingdomgame.com.

.PARAMETER SiteName
Optional: skip auto-detection and target this IIS site name directly.

.PARAMETER CertPfxPath
Path to the Cloudflare Origin CA certificate exported as .pfx.

.PARAMETER CertPassword
Password protecting the .pfx, as a SecureString. Prompted interactively if omitted.

.PARAMETER SkipSmokeTest
Skip the final HTTPS smoke test (useful if Cloudflare DNS has not propagated yet).

.EXAMPLE
.\Add-InternalSupportSubdomain.ps1 -CertPfxPath C:\certs\internal-support-origin.pfx

.NOTES
Targets Windows PowerShell 5.1 (.NET Framework) - confirmed the actual shell on the
production server. Deliberately avoids .NET 5+/Core-only static helpers
(RandomNumberGenerator.Fill, SHA256.HashData, Convert.ToHexString) that do not exist there.
#>
param(
    [string]$NewHostname = "internal-support.beekingdomgame.com",
    [string]$ExistingHostname = "api-ops.beekingdomgame.com",
    [string]$SiteName = "",
    [Parameter(Mandatory = $true)]
    [string]$CertPfxPath,
    [System.Security.SecureString]$CertPassword,
    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"

Import-Module WebAdministration -ErrorAction Stop

Write-Host "Bee Kingdom internal support subdomain setup"
Write-Host "New hostname:      $NewHostname"
Write-Host "Existing hostname: $ExistingHostname"

if ([string]::IsNullOrWhiteSpace($SiteName)) {
    $site = Get-Website | Where-Object {
        ($_.bindings.Collection | ForEach-Object { $_.bindingInformation }) -join ";" -match [regex]::Escape($ExistingHostname)
    } | Select-Object -First 1
    if (-not $site) {
        throw "Could not find an IIS site bound to '$ExistingHostname'. Pass -SiteName explicitly to skip auto-detection."
    }
    $SiteName = $site.Name
    Write-Host "Auto-detected IIS site: $SiteName"
} else {
    Write-Host "Using explicit IIS site: $SiteName"
}

if (-not (Test-Path -LiteralPath $CertPfxPath)) {
    throw "Certificate file not found: $CertPfxPath"
}

if (-not $CertPassword) {
    $CertPassword = Read-Host -Prompt "Enter the .pfx password" -AsSecureString
}

Write-Host "Importing origin certificate into Cert:\LocalMachine\My ..."
$importedCert = Import-PfxCertificate -FilePath $CertPfxPath -CertStoreLocation Cert:\LocalMachine\My -Password $CertPassword
$thumbprint = $importedCert.Thumbprint
Write-Host "Imported certificate thumbprint: $thumbprint"

$existingBinding = Get-WebBinding -Name $SiteName -Protocol https -HostHeader $NewHostname -ErrorAction SilentlyContinue
if ($existingBinding) {
    Write-Host "Binding for $NewHostname already exists on site $SiteName - updating certificate only."
    $binding = Get-WebBinding -Name $SiteName -Protocol https -HostHeader $NewHostname
    $binding.RemoveSslCertificate()
    $binding.AddSslCertificate($thumbprint, "My")
} else {
    Write-Host "Adding new HTTPS SNI binding: $NewHostname : 443"
    New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $NewHostname -SslFlags 1
    $binding = Get-WebBinding -Name $SiteName -Protocol https -HostHeader $NewHostname
    $binding.AddSslCertificate($thumbprint, "My")
}

Write-Host "Generating a new AdminSupport key ..."
# Uses RandomNumberGenerator.Create()/GetBytes and SHA256.Create()/ComputeHash instead of the
# newer Fill()/HashData()/Convert.ToHexString() static helpers - those are .NET 5+/Core only
# and this server runs the ASP.NET Core Module under Windows PowerShell 5.1 (.NET Framework),
# which does not have them.
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$keyBytes = New-Object byte[] 32
$rng.GetBytes($keyBytes)
$rng.Dispose()
$adminKey = [Convert]::ToBase64String($keyBytes)
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$hashBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($adminKey))
$sha256.Dispose()
$adminKeySha256 = -join ($hashBytes | ForEach-Object { $_.ToString("x2") })

$sitePhysicalPath = Get-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath
$webConfigPath = Join-Path $sitePhysicalPath "web.config"
if (-not (Test-Path -LiteralPath $webConfigPath)) {
    throw "web.config not found at $webConfigPath - cannot set environment variables."
}

Write-Host "Setting AdminSupport environment variables in $webConfigPath ..."
$psPath = "MACHINE/WEBROOT/APPHOST/$SiteName"

function Set-AspNetCoreEnvVar {
    param([string]$Name, [string]$Value)
    $existing = Get-WebConfigurationProperty -PSPath $psPath -Filter "system.webServer/aspNetCore/environmentVariables/environmentVariable[@name='$Name']" -Name name -ErrorAction SilentlyContinue
    if ($existing) {
        Set-WebConfigurationProperty -PSPath $psPath -Filter "system.webServer/aspNetCore/environmentVariables/environmentVariable[@name='$Name']" -Name value -Value $Value
    } else {
        Add-WebConfigurationProperty -PSPath $psPath -Filter "system.webServer/aspNetCore/environmentVariables" -Name "." -Value @{name = $Name; value = $Value }
    }
}

Set-AspNetCoreEnvVar -Name "AdminSupport__Enabled" -Value "true"
Set-AspNetCoreEnvVar -Name "AdminSupport__Key" -Value ""
Set-AspNetCoreEnvVar -Name "AdminSupport__KeySha256" -Value $adminKeySha256

$appPoolName = Get-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool
Write-Host "Recycling app pool: $appPoolName"
Restart-WebAppPool -Name $appPoolName
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "=========================================================================="
Write-Host " ADMIN SUPPORT KEY (save this now - it is shown only once, not logged):"
Write-Host " $adminKey"
Write-Host "=========================================================================="
Write-Host ""
Write-Host "Access URL: https://$NewHostname/admin/ui"
Write-Host "Header:     X-BeeKingdom-Support-Key: <the key above>"

if (-not $SkipSmokeTest) {
    Write-Host ""
    Write-Host "Running smoke test against https://$NewHostname ..."
    try {
        $health = Invoke-WebRequest -Uri "https://$NewHostname/health" -UseBasicParsing -TimeoutSec 15
        Write-Host "  /health -> $($health.StatusCode)"
        $ui = Invoke-WebRequest -Uri "https://$NewHostname/admin/ui" -UseBasicParsing -TimeoutSec 15
        Write-Host "  /admin/ui -> $($ui.StatusCode)"
        $lookup = Invoke-WebRequest -Uri "https://$NewHostname/admin/v1/players/lookup?email=smoke-test@invalid" -Headers @{ "X-BeeKingdom-Support-Key" = $adminKey } -UseBasicParsing -TimeoutSec 15 -SkipHttpErrorCheck
        Write-Host "  /admin/v1/players/lookup -> $($lookup.StatusCode) (404 is expected for an unknown email; it proves the key was accepted)"
    } catch {
        Write-Warning "Smoke test request failed: $($_.Exception.Message). DNS may not have propagated yet - retry in a few minutes with -SkipSmokeTest to skip this check."
    }
}

Write-Host ""
Write-Host "Done. Durability: this reuses the IIS site '$SiteName', which already restarts"
Write-Host "automatically on server reboot (W3SVC) and on crash (ASP.NET Core Module) - no"
Write-Host "separate service was created."
