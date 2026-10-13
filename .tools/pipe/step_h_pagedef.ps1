# Dump page definition keys and layout structure details for test payload design
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$raw = [System.IO.File]::ReadAllText((Join-Path $dir 'transferprice_v4_raw.json'))
$j = $raw | ConvertFrom-Json

$p = $j.'Costing Ref BOM Maintenance'
Write-Output ("PAGE wrapper keys: {0}" -f (($p.PSObject.Properties | ForEach-Object { $_.Name }) -join ', '))
Write-Output ("PAGE wrapper: Name={0} Version={1} Operation={2}" -f $p.Name, $p.Version, $p.Operation)
$pv = $p.Diffs[0].Value
Write-Output ("PAGE DEF keys (13): {0}" -f (($pv.PSObject.Properties | ForEach-Object { $_.Name }) -join ', '))
foreach ($k in $pv.PSObject.Properties) {
  $v = $k.Value
  if ($v -is [System.Array]) { $s = "ARRAY n=$($v.Count)" }
  elseif ($v -is [System.Management.Automation.PSCustomObject]) { $s = "OBJ keys[$(($v.PSObject.Properties.Name) -join ',')]" }
  else { $s = "$v"; if ($s.Length -gt 60) { $s = $s.Substring(0,60) + '...' } }
  Write-Output ("  {0} = {1}" -f $k.Name, $s)
}
# widgets sample
if ($pv.widgets) {
  Write-Output ("widgets[0] keys: {0}" -f (($pv.widgets[0].PSObject.Properties.Name) -join ', '))
}
Write-Output ''
# layout Diffs[9] component value keys (the tabstrip Add)
$comp = $j.layout.Diffs[8].Value
Write-Output ("layout comp-Add value keys: {0}" -f (($comp.PSObject.Properties.Name) -join ', '))
Write-Output ("  sourceTypeId={0} parentId={1}" -f $comp.sourceTypeId, $comp.parentId)
Write-Output ("  model keys: {0}" -f (($comp.model.PSObject.Properties.Name) -join ','))
