$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
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

function Unwrap($ex) { $e = $ex; while ($e.InnerException) { $e = $e.InnerException }; return $e.Message }

# 1) initialize
$i = Post-Mcp '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"pipe","version":"1.0"}}}'
Write-Output ("INIT status={0} sid={1}" -f $i.Status, $i.Sid)
if ($i.Status -ne 200 -or -not $i.Sid) { throw 'initialize failed' }
$sid = $i.Sid
$client.DefaultRequestHeaders.TryAddWithoutValidation('Mcp-Session-Id', $sid) | Out-Null

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

# 2) initialized notification
try {
  $n = Post-Mcp '{"jsonrpc":"2.0","method":"notifications/initialized"}'
  Write-Output ("NOTIF status={0}" -f $n.Status)
} catch { Write-Output ("NOTIF_FAIL: " + (Unwrap $_.Exception)) }

# 3) environment sanity
try {
  $env2 = Invoke-Tool 'GetEpicorEnvironment' @{}
  Write-Output ("ENV: " + $env2.Substring(0, [Math]::Min(300, $env2.Length)))
} catch { Write-Output ("ENV_FAIL: " + (Unwrap $_.Exception)) }

# 4) GetApp schema
try {
  $sc = Invoke-Tool 'GetMethodSchema' @{ serviceName = 'Ice.Services.Lib.MetaFXSvc'; methodName = 'GetApp' }
  [System.IO.File]::WriteAllText("$dir\getapp_schema.json", $sc, [System.Text.Encoding]::UTF8)
  Write-Output ("SCHEMA_OK len=" + $sc.Length)
} catch { Write-Output ("SCHEMA_FAIL: " + (Unwrap $_.Exception)) }
