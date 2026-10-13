$base = "http://localhost:5000/"
$hdr = @{ Accept = "application/json, text/event-stream" }
$r1 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}' -UseBasicParsing
$sid = $r1.Headers["mcp-session-id"]
if ($sid -is [array]) { $sid = $sid[0] }
$hdr2 = @{ Accept = "application/json, text/event-stream"; "Mcp-Session-Id" = $sid }
Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -UseBasicParsing | Out-Null

$args = @{ company = "GPEHZ"; viewId = "Ice.UIDbd.TEST-a"; layerName = "WhitelistProbe"; typeCode = "KNTCCustLayer"; confirm = $true }
$payload = @{ jsonrpc = "2.0"; id = 2; method = "tools/call"; params = @{ name = "DeleteKineticLayer"; arguments = $args } } | ConvertTo-Json -Depth 6 -Compress
$bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
$r3 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body $bytes -UseBasicParsing
$txt = $r3.Content
if ($txt -match '(?s)data:\s*(\{.*)') { $txt = $Matches[1] }
$j = $txt | ConvertFrom-Json
Write-Output ("RESULT: " + $j.result.content[0].text)
Write-Output "DELETE_DONE"
