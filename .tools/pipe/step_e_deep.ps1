# Deep analysis: per-diff item details, event dedup potential, page diff values
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$raw = [System.IO.File]::ReadAllText((Join-Path $dir 'transferprice_v4_raw.json'))
$j = $raw | ConvertFrom-Json

function ValSummary($v) {
  if ($null -eq $v) { return 'null' }
  if ($v -is [System.Management.Automation.PSCustomObject]) {
    $s = ($v | ConvertTo-Json -Compress -Depth 3)
    if ($s.Length -gt 100) { $s = $s.Substring(0,100) + '...' }
    return ("OBJ({0} keys){1}" -f $v.PSObject.Properties.Name.Count, $s)
  }
  $s = "$v"
  if ($s.Length -gt 100) { $s = $s.Substring(0,100) + '...' }
  return $s
}

Write-Output '=== layout.Diffs (10) ==='
$i = 0
foreach ($d in $j.layout.Diffs) {
  $i++
  Write-Output ("[{0}] op={1} id='{2}' path='{3}' idx={4}" -f $i, $d.Operation, $d.Id, $d.Path, $d.Index)
  Write-Output ("      value: {0}" -f (ValSummary $d.Value))
}

Write-Output ''
Write-Output '=== events.Diffs (208) - key identity analysis ==='
$ev = @()
$i = 0
foreach ($d in $j.events.Diffs) {
  $i++
  $names = ($d.PSObject.Properties.Name) -join ','
  $ev += [pscustomobject]@{ I=$i; Op=$d.Operation; Id=$d.Id; Path=$d.Path; Idx=$d.Index; Keys=$names; VLen=(($d.Value | ConvertTo-Json -Compress -Depth 30).Length) }
}
# group by Id
$g1 = $ev | Group-Object Id | Where-Object Count -gt 1
Write-Output ("by-Id dup groups: {0}" -f ($g1 | Measure-Object).Count)
$g1 | Select-Object -First 8 | ForEach-Object { Write-Output ("  id='{0}' x{1}" -f $_.Name, $_.Count) }
# group by Id+Path
$g2 = $ev | Group-Object { "{0}|{1}" -f $_.Id, $_.Path } | Where-Object Count -gt 1
Write-Output ("by-Id+Path dup groups: {0}" -f ($g2 | Measure-Object).Count)
$g2 | Select-Object -First 8 | ForEach-Object { Write-Output ("  '{0}' x{1}" -f $_.Name, $_.Count) }
# sample first 3 events
$ev | Select-Object -First 3 | ForEach-Object { Write-Output ("  [{0}] op={1} id='{2}' path='{3}' keys={4} vlen={5}" -f $_.I, $_.Op, $_.Id, $_.Path, $_.Keys, $_.VLen) }

Write-Output ''
Write-Output '=== dataviews.Diffs (32) ==='
$i = 0
foreach ($d in $j.dataviews.Diffs) {
  $i++
  if ($i -le 5 -or $i -gt 30) { Write-Output ("[{0}] op={1} id='{2}' path='{3}'" -f $i, $d.Operation, $d.Id, $d.Path) }
}
$dvIds = $j.dataviews.Diffs | ForEach-Object { $_.Id }
$dvDup = $dvIds | Group-Object | Where-Object Count -gt 1
Write-Output ("dataview by-Id dup groups: {0}" -f ($dvDup | Measure-Object).Count)
$dvDup | ForEach-Object { Write-Output ("  id='{0}' x{1}" -f $_.Name, $_.Count) }

Write-Output ''
Write-Output '=== tools.Diffs (7) ==='
foreach ($d in $j.tools.Diffs) { Write-Output ("op={0} id='{1}' path='{2}'" -f $d.Operation, $d.Id, $d.Path) }

Write-Output ''
Write-Output '=== one page sample: Costing Ref BOM Maintenance ==='
$p = $j.'Costing Ref BOM Maintenance'
Write-Output ("Name={0} Version={1} Operation={2}" -f $p.Name, $p.Version, $p.Operation)
foreach ($d in $p.Diffs) {
  Write-Output ("op={0} id='{1}' path='{2}'" -f $d.Operation, $d.Id, $d.Path)
  Write-Output ("      value: {0}" -f (ValSummary $d.Value))
}

Write-Output ''
Write-Output '=== layout top-level extra props ==='
foreach ($p in $j.layout.PSObject.Properties) {
  if ($p.Name -ne 'Diffs') { Write-Output ("layout.{0} = {1}" -f $p.Name, (ValSummary $p.Value)) }
}
