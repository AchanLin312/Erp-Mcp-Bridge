# step_z: UpdateKineticLayer -> restore layer description (bridge now supports description param)
$ErrorActionPreference = 'Stop'
$pipe = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'

# merged content (identity already fixed to TEST-a baseline)
$cj = [System.IO.File]::ReadAllText("$pipe\transfer_content_v4_fixed.json")
# sanity: must be valid JSON object
$null = $cj | ConvertFrom-Json | Out-Null
Write-Output ("contentJson chars: {0}" -f $cj.Length)

$base = "http://localhost:5000/"
$hdr = @{ Accept = "application/json, text/event-stream" }
$r1 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}' -UseBasicParsing -TimeoutSec 600
$sid = $r1.Headers["mcp-session-id"]
if ($sid -is [array]) { $sid = $sid[0] }
$hdr2 = @{ Accept = "application/json, text/event-stream"; "Mcp-Session-Id" = $sid }
Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -UseBasicParsing -TimeoutSec 600 | Out-Null
Write-Output "MCP session ready: $sid"

$toolArgs = @{
  company = "GPEHZ"
  viewId = "Ice.UIDbd.TEST-a"
  layerName = "TransferPrice V4"
  typeCode = "KNTCCustLayer"
  contentJson = $cj
  comment = "Restore layer description cleared by earlier metadata update"
  allowUnknownComponents = $true
  description = "Clean rebuilt copy of TransferPrice V4 (rebased on TEST-a)"
}
$payload = @{ jsonrpc = "2.0"; id = 2; method = "tools/call"; params = @{ name = "UpdateKineticLayer"; arguments = $toolArgs } } | ConvertTo-Json -Depth 6 -Compress
$bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
Write-Output ("RPC body chars: {0}" -f $payload.Length)

$r3 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body $bytes -UseBasicParsing -TimeoutSec 600
$txt = $r3.Content
if ($txt -match '(?s)data:\s*(\{.*)') { $txt = $Matches[1] }
$j = $txt | ConvertFrom-Json
if ($j.error) { Write-Output ("RPC ERROR: " + ($j.error | ConvertTo-Json -Compress -Depth 5)); throw 'rpc error' }
$t = $j.result.content[0].text
Write-Output ("TOOL RESULT: {0}" -f $t.Substring(0, [Math]::Min(3000, $t.Length)))
Write-Output "STEP_Z_DONE"
