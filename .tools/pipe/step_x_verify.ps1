# step_x: read back TEST-a + new layer, compare with original expansion
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
$target = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\test_a_v4_expanded.json'

$bodyObj = @{ request = @{ id = 'Ice.UIDbd.TEST-a'; properties = @{ deviceType = 'Desktop'; mode = 'AppStudio'; applicationType = 'view'; layers = @('TransferPrice V4'); additionalContext = @{ inPreviewMode = $true } } } }
$json = $bodyObj | ConvertTo-Json -Depth 12 -Compress
try {
  Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType 'application/json' -Headers $headers -UseBasicParsing -TimeoutSec 300 -OutFile $target
  Write-Host ("READBACK: 200 OK -> {0} bytes" -f (Get-Item $target).Length)
} catch {
  $bodyTxt = ''
  try { $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); $bodyTxt = $sr.ReadToEnd() } catch { $bodyTxt = '<no body>' }
  Write-Host ("READBACK FAIL: {0} :: {1}" -f $_.Exception.Message, $bodyTxt)
  exit 1
}

# ---- structural comparison ----
$new = (Get-Content $target -Raw -Encoding UTF8 | ConvertFrom-Json).returnObj
$old = (Get-Content 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\fullapp_v4_expanded.json' -Raw -Encoding UTF8 | ConvertFrom-Json).returnObj

function TopKeys($o) { $o.PSObject.Properties.Name | Sort-Object }

Write-Host "`n=== TOP-LEVEL KEYS ==="
$ok = TopKeys $old; $nk = TopKeys $new
Write-Host ("OLD: {0}" -f ($ok -join ','))
Write-Host ("NEW: {0}" -f ($nk -join ','))

Write-Host "`n=== PAGES ==="
Write-Host ("OLD count: {0}  NEW count: {1}" -f $old.Pages.Count, $new.Pages.Count)
for ($i = 0; $i -lt [Math]::Max($old.Pages.Count, $new.Pages.Count); $i++) {
  $op = $old.Pages[$i]; $np = $new.Pages[$i]
  $os = if ($op) { "{0} / {1} / comps={2}" -f $op.name, $op.viewType, $op.components.Count } else { '(missing)' }
  $ns = if ($np) { "{0} / {1} / comps={2}" -f $np.name, $np.viewType, $np.components.Count } else { '(missing)' }
  $match = if ($op -and $np -and $op.name -eq $np.name -and $op.components.Count -eq $np.components.Count) { 'OK' } else { '<< DIFF' }
  Write-Host ("[{0}] OLD {1}  |  NEW {2}  {3}" -f $i, $os, $ns, $match)
}

Write-Host "`n=== COLLECTIONS ==="
foreach ($k in @('Events','ToolBar','DataViews','Rules')) {
  $oc = if ($old.$k) { @($old.$k).Count } else { 0 }
  $nc = if ($new.$k) { @($new.$k).Count } else { 0 }
  $mark = if ($oc -eq $nc) { 'OK' } else { '<< DIFF' }
  Write-Host ("{0}: OLD={1} NEW={2} {3}" -f $k, $oc, $nc, $mark)
}

Write-Host "`n=== LAYOUT ==="
$ol = ($old.Layout | ConvertTo-Json -Depth 20 -Compress).Length
$nl = ($new.Layout | ConvertTo-Json -Depth 20 -Compress).Length
Write-Host ("Layout JSON size: OLD={0} NEW={1}" -f $ol, $nl)

Write-Host "`n=== PageDiagram SURVIVAL CHECK (pages[5]) ==="
$p5old = ($old.Pages[5] | ConvertTo-Json -Depth 30 -Compress)
$p5new = ($new.Pages[5] | ConvertTo-Json -Depth 30 -Compress)
$od = ([regex]::Matches($p5old, 'PageDiagram')).Count
$nd = ([regex]::Matches($p5new, 'PageDiagram')).Count
Write-Host ("pages[5] PageDiagram occurrences: OLD={0} NEW={1}" -f $od, $nd)
Write-Host ("pages[5] JSON size: OLD={0} NEW={1}" -f $p5old.Length, $p5new.Length)

Write-Host "`nVERIFY_DONE"
