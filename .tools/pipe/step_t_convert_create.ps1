# step_t: convert expanded app -> layer contentJson -> CreateKineticLayer on TEST-a
$ErrorActionPreference = 'Stop'
$pipe = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'

$nsDll = Get-ChildItem 'D:\MCP\Erp-Mcp-Bridge' -Recurse -Filter 'Newtonsoft.Json.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $nsDll) { throw 'Newtonsoft.Json.dll not found under bridge dir' }
Add-Type -Path $nsDll.FullName
Write-Output ("Newtonsoft loaded from: {0}" -f $nsDll.FullName)

# ---- 1. Convert ----
$raw = [System.IO.File]::ReadAllText("$pipe\fullapp_v4_expanded.json")
$root = [Newtonsoft.Json.Linq.JObject]::Parse($raw)
$ro = $root['returnObj']

$content = New-Object Newtonsoft.Json.Linq.JObject
$content['layout']    = $ro['Layout'].DeepClone()
$content['pages']     = $ro['Pages'].DeepClone()
$content['events']    = $ro['Events'].DeepClone()
$content['tools']     = $ro['ToolBar'].DeepClone()
$content['dataviews'] = $ro['DataViews'].DeepClone()
$content['rules']     = $ro['Rules'].DeepClone()
$cj = $content.ToString([Newtonsoft.Json.Formatting]::None)
[System.IO.File]::WriteAllText("$pipe\transfer_content_v4.json", $cj, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("contentJson chars: {0}" -f $cj.Length)
Write-Output ("content top keys: {0}" -f (($content.PSObject.Properties | ForEach-Object { $_.Name }) -join ','))

# ---- 2. MCP call ----
Add-Type -AssemblyName System.Net.Http
$base = 'http://localhost:5000/'
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(600)
$client.DefaultRequestHeaders.TryAddWithoutValidation('Accept', 'application/json, text/event-stream') | Out-Null

function Post-Mcp([string]$body) {
  $c = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, 'application/json')
  $resp = $client.PostAsync($base, $c).Result
  $bytes = $resp.Content.ReadAsByteArrayAsync().Result
  $txt = [System.Text.Encoding]::UTF8.GetString($bytes)
  $sid = $null
  if ($resp.Headers.Contains('mcp-session-id')) { $sid = ($resp.Headers.GetValues('mcp-session-id') | Select-Object -First 1) }
  [pscustomobject]@{ Status = [int]$resp.StatusCode; Sid = $sid; Body = $txt }
}

$init = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"pipe","version":"1.0"}}}'
$i = Post-Mcp $init
if ($i.Status -ne 200 -or -not $i.Sid) { throw ("initialize failed: {0} {1}" -f $i.Status, $i.Body.Substring(0, [Math]::Min(300, $i.Body.Length))) }
$sid = $i.Sid
$client.DefaultRequestHeaders.TryAddWithoutValidation('Mcp-Session-Id', $sid) | Out-Null
Post-Mcp '{"jsonrpc":"2.0","method":"notifications/initialized"}' | Out-Null
Write-Output "MCP session ready: $sid"

$toolArgs = New-Object Newtonsoft.Json.Linq.JObject
$toolArgs['company'] = 'GPEHZ'
$toolArgs['viewId'] = 'Ice.UIDbd.TEST-a'
$toolArgs['layerName'] = 'TransferPrice V4'
$toolArgs['typeCode'] = 'KNTCCustLayer'
$toolArgs['description'] = 'Clean rebuilt copy of TransferPrice V4 (rebased on TEST-a)'
$toolArgs['wip'] = $true
$toolArgs['contentJson'] = $cj

$toolParams = New-Object Newtonsoft.Json.Linq.JObject
$toolParams['name'] = 'CreateKineticLayer'
$toolParams['arguments'] = $toolArgs
$rpc = New-Object Newtonsoft.Json.Linq.JObject
$rpc['jsonrpc'] = '2.0'
$rpc['id'] = 2
$rpc['method'] = 'tools/call'
$rpc['params'] = $toolParams
$body = $rpc.ToString([Newtonsoft.Json.Formatting]::None)
Write-Output ("RPC body chars: {0}" -f $body.Length)

$r = Post-Mcp $body
if ($r.Status -ne 200) { Write-Output ("HTTP {0}: {1}" -f $r.Status, $r.Body.Substring(0, [Math]::Min(1500, $r.Body.Length))); throw 'http fail' }
$data = ($r.Body -split "`n" | Where-Object { $_ -match '^data:' } | ForEach-Object { $_.Substring(5).Trim() }) -join ''
$jr = $data | ConvertFrom-Json
if ($jr.error) { Write-Output ("RPC ERROR: " + ($jr.error | ConvertTo-Json -Compress -Depth 5)); throw 'rpc error' }
$resultText = $jr.result.content[0].text
Write-Output ("TOOL RESULT: {0}" -f $resultText.Substring(0, [Math]::Max(0, [Math]::Min(3000, $resultText.Length))))
Write-Output 'STEP_T_DONE'
