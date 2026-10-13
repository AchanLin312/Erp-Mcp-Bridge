# Fetch GetApp for dsTransPriceUpdate + TransferPrice V4 (try body variants until 200)
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
$out = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\fullapp_v4_expanded.json'

function Try-Variant([string]$label, [hashtable]$props) {
  $bodyObj = @{ request = @{ id = 'Ice.UIDbd.dsTransPriceUpdate'; properties = $props } }
  $json = $bodyObj | ConvertTo-Json -Depth 12 -Compress
  try {
    Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300 -OutFile $out
    $len = (Get-Item $out).Length
    Write-Host ("VARIANT OK [{0}] -> {1} bytes" -f $label, $len)
    return $true
  } catch {
    $bodyTxt = ''
    try {
      $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $bodyTxt = $sr.ReadToEnd()
      if ($bodyTxt.Length -gt 200) { $bodyTxt = $bodyTxt.Substring(0, 200) }
    } catch {}
    Write-Host ("VARIANT FAILED [{0}]: {1} {2}" -f $label, $_.Exception.Message, ($bodyTxt -replace '\s+', ' '))
    return $false
  }
}

# Variant 1: layers + empty company
$ok = Try-Variant 'layers+co-empty' @{ deviceType = 'Desktop'; userName = $user; company = ''; mode = 'AppStudio'; layers = @('TransferPrice V4') }
if (-not $ok) {
  # Variant 2: layers + GPEHZ company
  $ok = Try-Variant 'layers+co-GPEHZ' @{ deviceType = 'Desktop'; userName = $user; company = 'GPEHZ'; mode = 'AppStudio'; layers = @('TransferPrice V4') }
}
if (-not $ok) {
  # Variant 3: layerType + layers
  $ok = Try-Variant 'layerType+layers' @{ deviceType = 'Desktop'; userName = $user; company = 'GPEHZ'; mode = 'AppStudio'; layers = @('TransferPrice V4'); layerType = 'KNTCCustLayer' }
}
if ($ok) {
  $raw = Get-Content $out -Raw -Encoding UTF8
  Write-Host ("SIZE: {0}" -f $raw.Length)
  Write-Host ("HEAD: {0}" -f ($raw.Substring(0, [Math]::Min(400, $raw.Length)) -replace '\s+', ' '))
  Write-Host 'FETCH DONE'
} else {
  Write-Host 'ALL VARIANTS FAILED'
}
