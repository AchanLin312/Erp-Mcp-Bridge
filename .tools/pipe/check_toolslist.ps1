$port = $args[0]
if (-not $port) { $port = "5001" }
$base = "http://localhost:$port/"
try {
    $body1 = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}'
    $r1 = Invoke-WebRequest -Uri $base -Method Post -Headers @{Accept="application/json, text/event-stream"} -ContentType "application/json" -Body $body1 -UseBasicParsing -TimeoutSec 15
    $sid = $r1.Headers["mcp-session-id"]
    if ($sid -is [array]) { $sid = $sid[0] }
    Invoke-WebRequest -Uri $base -Method Post -Headers @{Accept="application/json, text/event-stream"; "Mcp-Session-Id"=$sid} -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -UseBasicParsing | Out-Null
    $r2 = Invoke-WebRequest -Uri $base -Method Post -Headers @{Accept="application/json, text/event-stream"; "Mcp-Session-Id"=$sid} -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' -UseBasicParsing -TimeoutSec 15
    $t = $r2.Content
    if ($t -match '(?s)data:\s*(\{.*)') { $t = $Matches[1] }
    $j = $t | ConvertFrom-Json
    Write-Output "tool count: $($j.result.tools.Count)"
    Write-Output ("has SyncEfxLibraryLocally: " + [bool]($j.result.tools | Where-Object { $_.name -eq "SyncEfxLibraryLocally" }))
} catch {
    Write-Output "FAILED: $($_.Exception.Message)"
}
