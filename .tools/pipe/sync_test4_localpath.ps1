$base = "http://localhost:5000/"
$hdr = @{ Accept = "application/json, text/event-stream" }
$r1 = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}' -UseBasicParsing
$sid = $r1.Headers["mcp-session-id"]
if ($sid -is [array]) { $sid = $sid[0] }
$hdr2 = @{ Accept = "application/json, text/event-stream"; "Mcp-Session-Id" = $sid }
Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -UseBasicParsing | Out-Null

# 模拟 coding agent 场景：显式传 localPath（当前工作区即项目目录）
$body = @{
    jsonrpc = "2.0"; id = 2; method = "tools/call"
    params  = @{ name = "SyncEfxLibraryLocally"; arguments = @{
        libraryId = "TestLibForAchen"
        localPath = "D:\epicor functions\efx.TestLibForAchen"
    } }
} | ConvertTo-Json -Depth 6 -Compress
$r = Invoke-WebRequest -Uri $base -Method Post -Headers $hdr2 -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 300
$txt = $r.Content
if ($txt -match '(?s)data:\s*(\{.*)') { $txt = $Matches[1] }
$j = $txt | ConvertFrom-Json
$j.result.content[0].text | Select-String -Pattern '"success"|"exitCode"|"durationSeconds"|"projectDir"|"gpeshellExe"|"error"' | ForEach-Object { $_.Line.Trim() }
Write-Output "PROBE_DONE"
