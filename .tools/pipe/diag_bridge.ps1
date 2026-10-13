Get-Process -Name ErpMcpBridge -ErrorAction SilentlyContinue | ForEach-Object {
    $cli = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine
    Write-Output "PID $($_.Id) | Start $($_.StartTime) | Path $($_.Path)"
    Write-Output "  CmdLine: $cli"
}
Write-Output "--- Listening ports for ErpMcpBridge PIDs ---"
$pids = (Get-Process -Name ErpMcpBridge -ErrorAction SilentlyContinue).Id
Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $pids -contains $_.OwningProcess } | ForEach-Object {
    Write-Output "PID $($_.OwningProcess) listening on $($_.LocalAddress):$($_.LocalPort)"
}
