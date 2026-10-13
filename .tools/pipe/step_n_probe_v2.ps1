# Probe v2 URL variants for MetaFX GetApp (with API key)
$ErrorActionPreference = 'Continue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$server = 'https://epicor-srv2019.gpe.com'
$env    = 'gpedev'
$user   = 'linyaochen'
$pass   = '15879361481LYC'
$apiKey = '2zj4lBIO46IrIvaCLHyxAsmuKcr1hiyMi0Se9XBnYSdd7'
$b64    = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($user):$($pass)"))
$headers = @{ 'Authorization' = "Basic $b64"; 'X-API-Key' = $apiKey; 'Accept' = 'application/json' }

$bodyObj = @{
  request = @{
    id = 'Ice.UIDbd.TEST-a'
    properties = @{ deviceType = 'Desktop'; userName = $user; company = ''; mode = 'AppStudio'; layers = @('') }
  }
}
$json = $bodyObj | ConvertTo-Json -Depth 12 -Compress

# TEST-a with no layers -> small response, safe probe
$urls = @(
  "$server/$env/api/v2/Ice/Lib/MetaFXSvc/GetApp",
  "$server/$env/api/v2/Ice/Lib/MetaFX/GetApp",
  "$server/$env/api/v2/GPEHZ/Ice/Lib/MetaFXSvc/GetApp"
)

foreach ($url in $urls) {
  Write-Host "=== $url"
  try {
    $resp = Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 120
    $len = $resp.Content.Length
    Write-Host ("  HTTP {0} len={1}" -f $resp.StatusCode, $len)
    $prev = $resp.Content
    if ($prev.Length -gt 600) { $prev = $prev.Substring(0, 600) }
    Write-Host ("  preview: {0}" -f $prev)
    break
  } catch {
    $msg = $_.Exception.Message
    $bodyTxt = ''
    try {
      $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $bodyTxt = $sr.ReadToEnd()
      if ($bodyTxt.Length -gt 300) { $bodyTxt = $bodyTxt.Substring(0, 300) }
    } catch {}
    Write-Host ("  FAILED {0} body={1}" -f $msg, $bodyTxt)
  }
}
