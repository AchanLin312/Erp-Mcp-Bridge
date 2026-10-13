Get-Process -Name ErpMcpBridge -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
$exe = "D:\MCP\Erp-Mcp-Bridge\bin\Debug\net10.0\ErpMcpBridge.exe"
$p = Start-Process -FilePath $exe -ArgumentList "--transport","http" -WorkingDirectory (Split-Path $exe) -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 4
$alive = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
if ($alive) {
    Write-Output "Started HTTP instance: PID $($p.Id)"
    Get-NetTCPConnection -State Listen -OwningProcess $p.Id -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "Listening: $($_.LocalAddress):$($_.LocalPort)" }
} else {
    Write-Output "Failed to start"
}
