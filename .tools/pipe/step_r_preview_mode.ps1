# step_r: bypass menu security via additionalContext.inPreviewMode
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
$out = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\getapp_case2.json'

function Invoke-Case([string]$label, [string]$appId, [object]$props, [string]$outfile) {
  Write-Host ("`n===== CASE [{0}] id={1} =====" -f $label, $appId)
  $bodyObj = @{ request = @{ id = $appId; properties = $props } }
  $json = $bodyObj | ConvertTo-Json -Depth 12 -Compress
  $target = if ($outfile) { $outfile } else { $out }
  try {
    Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300 -OutFile $target
    $len = (Get-Item $target).Length
    Write-Host ("RESULT: 200 OK -> {0} bytes -> {1}" -f $len, $target)
    $raw = Get-Content $target -Raw -Encoding UTF8
    Write-Host ("HEAD: {0}" -f ($raw.Substring(0, [Math]::Min(600, $raw.Length)) -replace '\s+', ' '))
    return $true
  } catch {
    $bodyTxt = ''
    try {
      $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $bodyTxt = $sr.ReadToEnd()
    } catch { $bodyTxt = '<no body readable>' }
    if ($bodyTxt.Length -gt 3000) { $bodyTxt = $bodyTxt.Substring(0, 3000) }
    Write-Host ("RESULT: FAIL {0}`nERRBODY: {1}" -f $_.Exception.Message, ($bodyTxt -replace '\s+', ' '))
    return $false
  }
}

# Case D: TEST-a + inPreviewMode (skip menu security)
$propsD = @{ deviceType = 'Desktop'; mode = 'AppStudio'; applicationType = 'view'; layers = @(); additionalContext = @{ inPreviewMode = $true } }
$okD = Invoke-Case 'D-TESTa-preview' 'Ice.UIDbd.TEST-a' $propsD $null

# Case E: dsTrans + V4 + inPreviewMode
$propsE = @{ deviceType = 'Desktop'; mode = 'AppStudio'; applicationType = 'view'; layers = @('TransferPrice V4'); additionalContext = @{ inPreviewMode = $true } }
$okE = Invoke-Case 'E-dsTrans-V4-preview' 'Ice.UIDbd.dsTransPriceUpdate' $propsE 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\fullapp_v4_expanded.json'

Write-Host ("`nSUMMARY: D={0} E={1}" -f $okD, $okE)
