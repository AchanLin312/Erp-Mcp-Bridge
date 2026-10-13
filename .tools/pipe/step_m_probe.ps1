# Probe GetApp URL variants
$ErrorActionPreference = 'Continue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$server = 'https://epicor-srv2019.gpe.com'
$env    = 'gpedev'
$user   = 'linyaochen'
$pass   = '15879361481LYC'
$apiKey = '2zj4lBIO46IrIvaCLHyxAsmuKcr1hiyMi0Se9XBnYSdd7'
$b64    = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($user):$($pass)"))

$bodyObj = @{
  request = @{
    id = 'Ice.UIDbd.dsTransPriceUpdate'
    properties = @{ deviceType = 'Desktop'; userName = $user; company = ''; mode = 'AppStudio'; layers = @('TransferPrice V4') }
  }
}
$json = $bodyObj | ConvertTo-Json -Depth 12 -Compress

$urls = @(
  "$server/$env/api/v1/Ice/Lib/MetaFX/GetApp",
  "$server/$env/api/v1/Ice/Lib/MetaFXSvc/GetApp",
  "$server/$env/api/v2/Ice/Lib/MetaFX/GetApp"
)

foreach ($url in $urls) {
  Write-Host "=== $url"
  foreach ($useKey in @($true, $false)) {
    $headers = @{ 'Authorization' = "Basic $b64"; 'Accept' = 'application/json' }
    if ($useKey) { $headers['X-API-Key'] = $apiKey }
    $tag = if ($useKey) { 'with-key' } else { 'no-key' }
    try {
      $resp = Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300
      Write-Host ("  [{0}] HTTP {1} len={2}" -f $tag, $resp.StatusCode, $resp.Content.Length)
      [System.IO.File]::WriteAllText('D:\MCP\Erp-Mcp-Bridge\.tools\pipe\fullapp_v4_expanded.json', $resp.Content, [System.Text.Encoding]::UTF8)
      Write-Host '  SAVED fullapp_v4_expanded.json'
      break
    } catch {
      $msg = $_.Exception.Message
      $bodyTxt = ''
      try {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $bodyTxt = $sr.ReadToEnd()
        if ($bodyTxt.Length -gt 400) { $bodyTxt = $bodyTxt.Substring(0, 400) }
      } catch {}
      Write-Host ("  [{0}] FAILED {1} body={2}" -f $tag, $msg, $bodyTxt)
    }
  }
}
