# Verify 'key' null root cause: pages must be page-definition array (lowercase name), layout must be full object
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$base = 'http://localhost:5000/'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(180)
$client.DefaultRequestHeaders.TryAddWithoutValidation('Accept', 'application/json, text/event-stream') | Out-Null

function Post-Mcp($body) {
  $c = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, 'application/json')
  $resp = $client.PostAsync($base, $c).Result
  $bytes = $resp.Content.ReadAsByteArrayAsync().Result
  $txt = [System.Text.Encoding]::UTF8.GetString($bytes)
  $sid = $null
  if ($resp.Headers.Contains('mcp-session-id')) { $sid = ($resp.Headers.GetValues('mcp-session-id') | Select-Object -First 1) }
  [pscustomobject]@{ Status = [int]$resp.StatusCode; Sid = $sid; Body = $txt }
}

$i = Post-Mcp '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"pipe","version":"1.0"}}}'
if ($i.Status -ne 200 -or -not $i.Sid) { throw ("initialize failed: {0} {1}" -f $i.Status, $i.Body) }
$sid = $i.Sid
$client.DefaultRequestHeaders.TryAddWithoutValidation('Mcp-Session-Id', $sid) | Out-Null
Post-Mcp '{"jsonrpc":"2.0","method":"notifications/initialized"}' | Out-Null

function Invoke-Tool($name, $argsObj) {
  $o = @{ jsonrpc = '2.0'; id = 2; method = 'tools/call'; params = @{ name = $name; arguments = $argsObj } }
  $j = $o | ConvertTo-Json -Depth 60 -Compress
  $r = Post-Mcp $j
  if ($r.Status -ne 200) { throw ("http {0}: {1}" -f $r.Status, $r.Body.Substring(0, [Math]::Min(500, $r.Body.Length))) }
  $data = ($r.Body -split "`n" | Where-Object { $_ -match '^data:' } | ForEach-Object { $_.Substring(5).Trim() }) -join ''
  $jr = $data | ConvertFrom-Json
  if ($jr.error) { throw ("rpc error: " + ($jr.error | ConvertTo-Json -Compress -Depth 5)) }
  return $jr.result.content[0].text
}

# page definition (NOT wrapper): lowercase name is what LayerTarget.Save reads
$guid = [guid]::NewGuid().ToString()
$pageDef = [ordered]@{
  name = 'JPage'; guid = $guid; caption = 'JPage'; viewType = 'TabPage'; pageType = ''
  enableFullWidth = $false; width = 1440; height = 0
  navigation = [ordered]@{ enable = $true; parentPage = ''; epBinding = ''; epBindingPeerOrder = ''; dataSetId = ''; pageCaption = 'JPage'; tabId = '' }
  components = @()
  flexLayout = $false; fitToPageMode = $false
  slidingPanel = [ordered]@{ hideClose = $false }
}
$content = [ordered]@{
  layout = [ordered]@{ caption = 'JVerify'; viewType = 'Tabs' }
  pages  = @($pageDef)
}
$cj = $content | ConvertTo-Json -Depth 60 -Compress
[System.IO.File]::WriteAllText((Join-Path $dir 'jverify_content.json'), $cj, [System.Text.Encoding]::UTF8)
Write-Output ("contentJson length: {0}" -f $cj.Length)

try {
  $out = Invoke-Tool 'CreateKineticLayer' @{
    company = 'GPEHZ'; viewId = 'Ice.UIDbd.TEST-a'; layerName = 'JVerify'
    typeCode = 'KNTCCustLayer'; description = 'verify page-def fmt'; wip = $true
    contentJson = $cj
  }
  Write-Output ("CREATE OK: {0}" -f $out)
} catch {
  Write-Output ("CREATE FAILED: {0}" -f $_.Exception.Message)
}
