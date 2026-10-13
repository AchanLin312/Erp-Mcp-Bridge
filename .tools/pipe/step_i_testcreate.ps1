# Experiment: create tiny layer with pages-array content on TEST-a via HTTP MCP pipe
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$base = 'http://localhost:5000/'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(120)
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
  $j = $o | ConvertTo-Json -Depth 40 -Compress
  $r = Post-Mcp $j
  if ($r.Status -ne 200) { throw ("http {0}: {1}" -f $r.Status, $r.Body.Substring(0, [Math]::Min(300, $r.Body.Length))) }
  $data = ($r.Body -split "`n" | Where-Object { $_ -match '^data:' } | ForEach-Object { $_.Substring(5).Trim() }) -join ''
  $jr = $data | ConvertFrom-Json
  if ($jr.error) { throw ("rpc error: " + ($jr.error | ConvertTo-Json -Compress -Depth 5)) }
  return $jr.result.content[0].text
}

# minimal page payload mirroring V4 shapes
$guid = [guid]::NewGuid().ToString()
$pageDef = [ordered]@{
  name = 'TestPage'; guid = $guid; caption = 'TestPage'; viewType = 'TabPage'; pageType = ''
  enableFullWidth = $false; width = 1440; height = 0
  navigation = [ordered]@{ enable = $true; parentPage = ''; epBinding = ''; epBindingPeerOrder = ''; dataSetId = ''; pageCaption = 'TestPage'; tabId = '' }
  components = @()
  flexLayout = $false; fitToPageMode = $false
  slidingPanel = [ordered]@{ hideClose = $false }
}
$pageWrapper = [ordered]@{
  Name = 'TestPage'; Version = '3.0'
  Diffs = @(@{ Id = ''; Path = ''; Value = $pageDef; Operation = 'Add'; Index = 0 })
  Operation = 'Add'
}
$content = [ordered]@{
  layout = [ordered]@{ Name = 'layout'; Version = '3.0'; Diffs = @(@{ Id = ''; Path = '/caption'; Value = 'Pipe Test'; Operation = 'Replace'; Index = 0 }); Operation = 'Replace' }
  pages = @($pageWrapper)
}
$cj = $content | ConvertTo-Json -Depth 40 -Compress
[System.IO.File]::WriteAllText((Join-Path $dir 'pipetest_content.json'), $cj, [System.Text.Encoding]::UTF8)

try {
  $out = Invoke-Tool 'CreateKineticLayer' @{
    company = 'GPEHZ'; viewId = 'Ice.UIDbd.dsTransPriceUpdate'; layerName = 'PipeTest2'
    typeCode = 'KNTCCustLayer'; description = 'pipe test page fmt'; wip = $true
  }
  Write-Output ("CREATE OK: {0}" -f $out)
} catch {
  Write-Output ("CREATE FAILED: {0}" -f $_.Exception.Message)
}
