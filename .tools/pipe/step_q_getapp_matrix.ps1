# step_q: GetApp isolation matrix with FULL error body capture
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
$out = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\getapp_case.json'

function Invoke-Case([string]$label, [string]$appId, [object]$props) {
  Write-Host ("`n===== CASE [{0}] id={1} =====" -f $label, $appId)
  $bodyObj = @{ request = @{ id = $appId; properties = $props } }
  $json = $bodyObj | ConvertTo-Json -Depth 12 -Compress
  Write-Host ("BODY: {0}" -f $json)
  try {
    Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300 -OutFile $out
    $len = (Get-Item $out).Length
    Write-Host ("RESULT: 200 OK -> {0} bytes" -f $len)
    $raw = Get-Content $out -Raw -Encoding UTF8
    Write-Host ("HEAD: {0}" -f ($raw.Substring(0, [Math]::Min(500, $raw.Length)) -replace '\s+', ' '))
    return $true
  } catch {
    $bodyTxt = ''
    try {
      $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
      $bodyTxt = $sr.ReadToEnd()
    } catch { $bodyTxt = '<no body readable: ' + $_.Exception.Message + '>' }
    Write-Host ("RESULT: FAIL {0}" -f $_.Exception.Message)
    if ($bodyTxt) {
      if ($bodyTxt.Length -gt 4000) { $bodyTxt = $bodyTxt.Substring(0, 4000) }
      Write-Host ("ERRBODY: {0}" -f ($bodyTxt -replace '\s+', ' '))
    }
    return $false
  }
}

$baseProps = @{ deviceType = 'Desktop'; mode = 'AppStudio'; applicationType = 'view'; layers = @() }

# Case A: TEST-a minimal, no layers
$okA = Invoke-Case 'A-TESTa-minimal' 'Ice.UIDbd.TEST-a' $baseProps

# Case B: dsTransPriceUpdate minimal, no layers
$okB = Invoke-Case 'B-dsTrans-minimal' 'Ice.UIDbd.dsTransPriceUpdate' $baseProps

# Case C: dsTransPriceUpdate + TransferPrice V4
$propsC = @{ deviceType = 'Desktop'; mode = 'AppStudio'; applicationType = 'view'; layers = @('TransferPrice V4') }
$okC = Invoke-Case 'C-dsTrans-V4' 'Ice.UIDbd.dsTransPriceUpdate' $propsC

Write-Host ("`nSUMMARY: A={0} B={1} C={2}" -f $okA, $okB, $okC)
