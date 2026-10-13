# Call Epicor REST GetApp: dsTransPriceUpdate + layer 'TransferPrice V4' -> full expanded content to disk
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'

$server = 'https://epicor-srv2019.gpe.com'
$env    = 'gpedev'
$user   = 'linyaochen'
$pass   = '15879361481LYC'
$apiKey = '2zj4lBIO46IrIvaCLHyxAsmuKcr1hiyMi0Se9XBnYSdd7'
$b64    = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($user):$($pass)"))

function Invoke-GetApp($apiVer, $viewId, $layers, $outFile) {
  $url = "$server/$env/api/$apiVer/Ice/Lib/MetaFX/GetApp"
  $bodyObj = @{
    request = @{
      id = $viewId
      properties = @{
        deviceType = 'Desktop'
        userName   = $user
        company    = ''
        mode       = 'AppStudio'
        layers     = $layers
      }
    }
  }
  $json = $bodyObj | ConvertTo-Json -Depth 12 -Compress
  Write-Host ("[{0}] GETAPP {1} layers={2}" -f $apiVer, $viewId, ($layers -join ','))
  try {
    $resp = Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' `
      -Headers @{ 'Authorization' = "Basic $b64"; 'X-API-Key' = $apiKey } `
      -UseBasicParsing -TimeoutSec 300
    $txt = $resp.Content
    [System.IO.File]::WriteAllText((Join-Path $dir $outFile), $txt, [System.Text.Encoding]::UTF8)
    Write-Host ("  HTTP {0}, length={1} chars -> {2}" -f $resp.StatusCode, $txt.Length, $outFile)
    return $txt
  } catch {
    Write-Host ("  FAILED: {0}" -f $_.Exception.Message)
    if ($_.Exception.Response) {
      try {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host ("  body: {0}" -f $sr.ReadToEnd().Substring(0, 500))
      } catch {}
    }
    return $null
  }
}

$txt = Invoke-GetApp 'v1' 'Ice.UIDbd.dsTransPriceUpdate' @('TransferPrice V4') 'fullapp_v4_expanded.json'
if ($txt) {
  $j = $txt | ConvertFrom-Json
  Write-Host ("  top-level keys: " + (($j.PSObject.Properties.Name | Select-Object -First 30) -join ', '))
} else {
  Write-Host "v1 failed, trying v2..."
  $txt2 = Invoke-GetApp 'v2' 'Ice.UIDbd.dsTransPriceUpdate' @('TransferPrice V4') 'fullapp_v4_expanded.json'
  if ($txt2) {
    $j2 = $txt2 | ConvertFrom-Json
    Write-Host ("  top-level keys: " + (($j2.PSObject.Properties.Name | Select-Object -First 30) -join ', '))
  }
}
