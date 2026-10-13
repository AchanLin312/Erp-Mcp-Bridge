$base = "http://localhost:5000/"
$hdr = @{ Accept = "application/json, text/event-stream" }
$r1 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}' -UseBasicParsing
$sid = $r1.Headers["mcp-session-id"]
if ($sid -is [array]) { $sid = $sid[0] }
$hdr2 = @{ Accept = "application/json, text/event-stream"; "Mcp-Session-Id" = $sid }
Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -UseBasicParsing | Out-Null
# error path: nonexistent libraryId
$body = @{
    jsonrpc = "2.0"; id = 2; method = "tools/call"
    params  = @{ name = "SyncEfxLibraryLocally"; arguments = @{ libraryId = "NoSuchLib12345" } }
} | ConvertTo-Json -Depth 6 -Compress
$r2 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 60
$txt = $r2.Content
if ($txt -match '(?s)data:\s*(\{.*)') { $txt = $Matches[1] }
$j = $txt | ConvertFrom-Json
Write-Output "=== error path result ==="
Write-Output $j.result.content[0].text
Write-Output "PROBE_DONE"
