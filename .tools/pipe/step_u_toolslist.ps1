$base = "http://localhost:5000/"
$hdr = @{ Accept = "application/json, text/event-stream" }
# initialize
$r1 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}' -UseBasicParsing
$sid = $r1.Headers["mcp-session-id"]
if ($sid -is [array]) { $sid = $sid[0] }
Write-Output "session: $sid"
$hdr2 = @{ Accept = "application/json, text/event-stream"; "Mcp-Session-Id" = $sid }
Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -UseBasicParsing | Out-Null
# tools/list
$r3 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' -UseBasicParsing
$txt = $r3.Content
if ($txt -match '(?s)data:\s*(\{.*)') { $txt = $Matches[1] }
$j = $txt | ConvertFrom-Json
$tools = $j.result.tools
Write-Output "tool count: $($tools.Count)"
$ck = $tools | Where-Object { $_.name -eq "CreateKineticLayer" }
Write-Output "CreateKineticLayer props:"
$ck.inputSchema.properties.PSObject.Properties | ForEach-Object { Write-Output ("  - " + $_.Name + " (" + $_.Value.type + ")") }
Write-Output "PROBE_DONE"
