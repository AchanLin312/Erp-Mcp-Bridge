# Fetch GetApp expanded content via the proven V2 odata URL
$ErrorActionPreference = 'Continue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$server = 'https://epicor-srv2019.gpe.com'
$envName = 'gpedev'
$user = 'linyaochen'
$pass = '15879361481LYC'
$apiKey = '2zj4lBIO46IrIvaCLHyxAsmuKcr1hiyMi0Se9XBnYSdd7'
$b64 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($user):$($pass)"))
$headers = @{ 'Authorization' = "Basic $b64"; 'X-API-Key' = $apiKey; 'Accept' = 'application/json' }

$url = "$server/$envName/api/V2/odata/GPEHZ/Ice.Lib.MetaFXSvc/GetApp"

function Invoke-GetApp([string]$viewId, [string[]]$layers, [string]$outFile) {
  $bodyObj = @{
    request = @{
      id = $viewId
      properties = @{
        deviceType = 'Desktop'; userName = $user; company = ''
        mode = 'AppStudio'; layers = $layers
      }
    }
  }
  $json = $bodyObj | ConvertTo-Json -Depth 12 -Compress
  try {
    if ($outFile) {
      Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300 -OutFile $outFile
      $len = (Get-Item $outFile).Length
      Write-Host ("OK {0} layers=[{1}] -> {2} ({3} bytes)" -f $viewId, ($layers -join ','), $outFile, $len)
      $prev = (Get-Content $outFile -Raw -Encoding UTF8)
      if ($prev.Length -gt 500) { $prev = $prev.Substring(0, 500) }
      Write-Host ("  preview: {0}" -f ($prev -replace '\s+', ' '))
      return $true
    } else {
      $resp = Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300
      Write-Host ("OK {0} layers=[{1}] HTTP {2} len={3}" -f $viewId, ($layers -join ','), $resp.StatusCode, $resp.Content.Length)
      return $true
    }
  } catch {
    $msg = $_.Exception.Message
    $bodyTxt = ''
    try {
      $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $bodyTxt = $sr.ReadToEnd()
      if ($bodyTxt.Length -gt 400) { $bodyTxt = $bodyTxt.Substring(0, 400) }
    } catch {}
    Write-Host ("FAILED {0} layers=[{1}]: {2} body={3}" -f $viewId, ($layers -join ','), $msg, ($bodyTxt -replace '\s+', ' '))
    return $false
  }
}

# Step 1: safe probe on TEST-a (small)
$ok = Invoke-GetApp 'Ice.UIDbd.TEST-a' @('') $null
if (-not $ok) { Write-Host 'PROBE FAILED - stop'; exit 1 }

# Step 2: fetch the real expanded content of dsTransPriceUpdate + TransferPrice V4
$out = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\fullapp_v4_expanded.json'
$ok2 = Invoke-GetApp 'Ice.UIDbd.dsTransPriceUpdate' @('TransferPrice V4') $out
if ($ok2) { Write-Host 'FETCH DONE' } else { Write-Host 'FETCH FAILED' }
