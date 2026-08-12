[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [ValidateRange(1, 365)]
    [int]$MinimumCertificateValidityDays = 14,

    [string]$ExpectedIssuerPattern = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
Add-Type -AssemblyName System.Security

try {
    $uri = [Uri]$BaseUrl
}
catch {
    throw 'BaseUrl doit etre une URL absolue HTTPS valide.'
}

if (-not $uri.IsAbsoluteUri -or $uri.Scheme -ne 'https') {
    throw 'Le preflight staging refuse toute URL non HTTPS.'
}
if (-not [string]::IsNullOrEmpty($uri.UserInfo) -or -not [string]::IsNullOrEmpty($uri.Query) -or -not [string]::IsNullOrEmpty($uri.Fragment)) {
    throw 'BaseUrl ne doit contenir ni credentials, query string ou fragment.'
}
if ($uri.AbsolutePath.TrimEnd('/') -ne '/chat/v1') {
    throw 'BaseUrl doit cibler exactement le prefixe versionne /chat/v1.'
}
if ($uri.IsLoopback) {
    throw 'Le preflight TLS staging ne cible pas le loopback; utiliser Test-ProductionLocal.ps1.'
}

$port = if ($uri.IsDefaultPort) { 443 } else { $uri.Port }
$tcp = [System.Net.Sockets.TcpClient]::new()
$ssl = $null
$client = $null
try {
    $connect = $tcp.ConnectAsync($uri.DnsSafeHost, $port)
    if (-not $connect.Wait([TimeSpan]::FromSeconds(10))) {
        throw 'Connexion TCP staging expiree.'
    }

    $ssl = [System.Net.Security.SslStream]::new($tcp.GetStream(), $false)
    # AuthenticateAsClient envoie le nom SNI et applique la validation de chaine
    # et de nom de l'hote du systeme d'exploitation.
    $ssl.AuthenticateAsClient($uri.DnsSafeHost)
    if (-not $ssl.IsAuthenticated -or -not $ssl.IsEncrypted) {
        throw 'La session TLS staging n’est pas authentifiee et chiffree.'
    }

    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($ssl.RemoteCertificate)
    $minimumExpiry = [DateTime]::UtcNow.AddDays($MinimumCertificateValidityDays)
    if ($certificate.NotAfter.ToUniversalTime() -lt $minimumExpiry) {
        throw "Le certificat expire avant la marge minimale de $MinimumCertificateValidityDays jours."
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedIssuerPattern) -and $certificate.Issuer -notmatch $ExpectedIssuerPattern) {
        throw 'L’emetteur du certificat ne correspond pas a la politique staging attendue.'
    }

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(10)
    $response = $client.GetAsync(($BaseUrl.TrimEnd('/') + '/capabilities')).GetAwaiter().GetResult()
    if (-not $response.IsSuccessStatusCode) {
        throw "Capabilities staging a retourne HTTP $([int]$response.StatusCode)."
    }
    if ($response.Headers.Location) {
        throw 'Capabilities staging ne doit pas rediriger.'
    }
    $cacheControl = [string]$response.Headers.CacheControl
    if ($cacheControl -notmatch 'no-store' -or $cacheControl -notmatch 'no-cache' -or $cacheControl -notmatch 'max-age=0' -or $cacheControl -notmatch 'must-revalidate') {
        throw "Capabilities staging cacheables: $cacheControl"
    }
    if ($response.Headers.Age -and $response.Headers.Age.TotalSeconds -gt 0) { throw "Capabilities staging servies depuis un cache (Age=$($response.Headers.Age))." }

    $json = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($json.protocolVersion -ne 'chat-v1') { throw 'ProtocolVersion staging incompatible.' }
    if ($json.provider -ne 'server') { throw 'Provider capabilities staging incompatible.' }
    if ($null -eq $json.server -or $null -eq $json.realtime -or $null -eq $json.limits) { throw 'Contrat capabilities staging incomplet.' }
    if ([int]$json.limits.bodyMaxCharacters -notin 1..4000 -or
        [int]$json.limits.messagesPerMinutePerPlayer -notin 1..600 -or
        [int]$json.limits.messagesPerTenSecondsPerConversation -notin 1..100 -or
        [int]$json.limits.privateConversationCreatesPerHour -notin 1..1000 -or
        [int]$json.limits.maxPrivateRecipients -notin 1..100) { throw 'Limites capabilities staging invalides.' }
    if ([int]$json.idempotencyReceiptRetentionDays -notin 2..3650) { throw 'Retention des recus staging incompatible.' }
    $expectedChannels = @('Alliance', 'Server', 'Private', 'Leaders')
    $actualChannels = @($json.channels | ForEach-Object { [string]$_ })
    if ($actualChannels.Count -eq 0 -or @($actualChannels | Sort-Object -Unique).Count -ne $actualChannels.Count -or @($actualChannels | Where-Object { $_ -notin $expectedChannels }).Count -gt 0) {
        throw 'Canaux capabilities staging invalides.'
    }

    # La négociation est la seule exception à l’authentification chat: toute
    # autre méthode sur cette route doit être refusée directement, sans session.
    $jsonMediaType = 'application/json'
    $capabilityMethodChecks = @(
        @{ Method = 'POST'; Body = '{}' },
        @{ Method = 'PUT'; Body = $null },
        @{ Method = 'DELETE'; Body = $null }
    )
    foreach ($check in $capabilityMethodChecks) {
        $methodRequest = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($check.Method), ($BaseUrl.TrimEnd('/') + '/capabilities'))
        if ($null -ne $check.Body) { $methodRequest.Content = [System.Net.Http.StringContent]::new($check.Body, [System.Text.Encoding]::UTF8, $jsonMediaType) }
        $methodResponse = $client.SendAsync($methodRequest).GetAwaiter().GetResult()
        if ([int]$methodResponse.StatusCode -lt 400 -or [int]$methodResponse.StatusCode -ge 500) { throw "Methode $($check.Method) inattendue sur capabilities: HTTP $([int]$methodResponse.StatusCode)." }
        if ($methodResponse.Headers.Location) { throw "Location interdite pour capabilities/$($check.Method)." }
        $methodResponse.Dispose(); $methodRequest.Dispose()
    }

    $probeId = [Guid]::NewGuid().ToString('D')
    $probes = @(
        @{ Method = 'GET'; Path = '/conversations?limit=1'; Body = $null },
        @{ Method = 'GET'; Path = "/conversations/$probeId/messages?afterSequence=0&limit=1"; Body = $null },
        @{ Method = 'POST'; Path = "/conversations/$probeId/messages"; Body = '{"clientRequestId":"preflight","body":"x","clientCreatedAt":"2026-01-01T00:00:00Z"}' },
        @{ Method = 'POST'; Path = "/conversations/$probeId/read"; Body = '{"sequence":0}' },
        @{ Method = 'POST'; Path = "/messages/$probeId/report"; Body = '{"clientRequestId":"preflight","category":"spam"}' },
        @{ Method = 'POST'; Path = "/messages/$probeId/translations"; Body = '{"targetLocale":"en-CA"}' }
    )
    foreach ($probe in $probes) {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($probe.Method), ($BaseUrl.TrimEnd('/') + $probe.Path))
        if ($null -ne $probe.Body) { $request.Content = [System.Net.Http.StringContent]::new($probe.Body, [System.Text.Encoding]::UTF8, $jsonMediaType) }
        $probeResponse = $client.SendAsync($request).GetAwaiter().GetResult()
        if ([int]$probeResponse.StatusCode -ge 300 -and [int]$probeResponse.StatusCode -le 399) { throw "Redirection interdite pour $($probe.Path): HTTP $([int]$probeResponse.StatusCode)." }
        if ($probeResponse.Headers.Location) { throw "Location interdite pour $($probe.Path)." }
        if ([int]$probeResponse.StatusCode -ne 401) { throw "La route authentifiee $($probe.Path) doit repondre directement 401 sans bearer, obtenu $([int]$probeResponse.StatusCode)." }
        $probeResponse.Dispose()
        $request.Dispose()
    }

    [pscustomobject]@{
        Success = $true
        Host = $uri.DnsSafeHost
        Port = $port
        TlsProtocol = $ssl.SslProtocol.ToString()
        CertificateNotAfterUtc = $certificate.NotAfter.ToUniversalTime().ToString('O')
        ProtocolVersion = $json.protocolVersion
        ServerEnabled = [bool]$json.server
        RealtimeEnabled = [bool]$json.realtime
        BodyMaxCharacters = [int]$json.limits.bodyMaxCharacters
        MaxPrivateRecipients = [int]$json.limits.maxPrivateRecipients
        IdempotencyReceiptRetentionDays = [int]$json.idempotencyReceiptRetentionDays
    } | ConvertTo-Json -Depth 3
}
finally {
    if ($client) { $client.Dispose() }
    if ($ssl) { $ssl.Dispose() }
    $tcp.Dispose()
}
