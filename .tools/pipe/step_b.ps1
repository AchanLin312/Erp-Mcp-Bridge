$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$base = 'http://localhost:5000/'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(300)
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
function Unwrap($ex) { $e = $ex; while ($e.InnerException) { $e = $e.InnerException }; return $e.Message }

$i = Post-Mcp '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"pipe","version":"1.0"}}}'
if ($i.Status -ne 200 -or -not $i.Sid) { throw 'initialize failed' }
$client.DefaultRequestHeaders.TryAddWithoutValidation('Mcp-Session-Id', $i.Sid) | Out-Null
Post-Mcp '{"jsonrpc":"2.0","method":"notifications/initialized"}' | Out-Null

function Invoke-Tool($name, $argsObj) {
  $o = @{ jsonrpc = '2.0'; id = 2; method = 'tools/call'; params = @{ name = $name; arguments = $argsObj } }
  $j = $o | ConvertTo-Json -Depth 20 -Compress
  $r = Post-Mcp $j
  if ($r.Status -ne 200) { throw ("http {0}: {1}" -f $r.Status, $r.Body.Substring(0, [Math]::Min(300, $r.Body.Length))) }
  $data = ($r.Body -split "`n" | Where-Object { $_ -match '^data:' } | ForEach-Object { $_.Substring(5).Trim() }) -join ''
  $jr = $data | ConvertFrom-Json
  if ($jr.error) { throw ("rpc error: " + ($jr.error | ConvertTo-Json -Compress -Depth 5)) }
  return $jr.result.content[0].text
}

# 1) find MetaFX service real name
try {
  $svcs = Invoke-Tool 'ListEpicorServices' @{}
  [System.IO.File]::WriteAllText("$dir\services.json", $svcs, [System.Text.Encoding]::UTF8)
  $lines = $svcs -split "`n" | Where-Object { $_ -match 'MetaFX|metafx' }
  Write-Output ("SERVICES_MATCH count={0}" -f $lines.Count)
  $lines | Select-Object -First 10 | ForEach-Object { Write-Output ("  " + $_.Trim()) }
} catch { Write-Output ("SERVICES_FAIL: " + (Unwrap $_.Exception)) }

# 2) GetApp on TEST-a (best-guess payload)
$payload = '{"request":{"id":"Ice.UIDbd.TEST-a","mode":"AppStudio","additionalContext":{"inPreviewMode":true}}}'
try {
  $r = Invoke-Tool 'CallBoMethod' @{ company = 'GPEHZ'; serviceName = 'Ice.Services.Lib.MetaFXSvc'; methodName = 'GetApp'; jsonPayload = $payload }
  [System.IO.File]::WriteAllText("$dir\testa_app.json", $r, [System.Text.Encoding]::UTF8)
  Write-Output ("GETAPP_TESTA_OK len=" + $r.Length)
  Write-Output ("  head: " + $r.Substring(0, [Math]::Min(400, $r.Length)))
} catch { Write-Output ("GETAPP_TESTA_FAIL: " + (Unwrap $_.Exception)) }
