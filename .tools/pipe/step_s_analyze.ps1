# step_s: analyze structure of fetched GetApp responses
$ErrorActionPreference = 'Continue'

function Analyze([string]$path, [string]$label) {
  Write-Host ("`n===== {0} ({1}) =====" -f $label, $path)
  $raw = Get-Content $path -Raw -Encoding UTF8
  Write-Host ("TOTAL CHARS: {0}" -f $raw.Length)
  $j = $raw | ConvertFrom-Json
  $ro = $j.returnObj
  if (-not $ro) { Write-Host 'NO returnObj!'; return }
  Write-Host 'returnObj top-level keys:'
  foreach ($p in $ro.PSObject.Properties) {
    $v = $p.Value
    $type = if ($null -eq $v) { 'null' } else { $v.GetType().Name }
    $size = 0
    $detail = ''
    if ($v -is [System.Management.Automation.PSCustomObject]) {
      $size = ($v | ConvertTo-Json -Depth 20 -Compress).Length
      $detail = 'keys=[' + (($v.PSObject.Properties.Name | Select-Object -First 12) -join ',') + ']'
      if ($v.PSObject.Properties.Name.Count -gt 12) { $detail = $detail -replace '\]$', ',...]More=' + $v.PSObject.Properties.Name.Count }
    } elseif ($v -is [System.Array]) {
      $size = ($v | ConvertTo-Json -Depth 20 -Compress).Length
      $detail = "count=$($v.Count)"
    } elseif ($null -ne $v) {
      $s = "$v"; if ($s.Length -gt 80) { $s = $s.Substring(0,80) + '...' }
      $detail = "value=$s"
    }
    Write-Host ("  {0,-22} {1,-12} jsonSize={2,-9} {3}" -f $p.Name, $type, $size, $detail)
  }

  # pages detail
  if ($ro.pages) {
    Write-Host "`npages detail:"
    if ($ro.pages -is [System.Array]) {
      foreach ($pg in $ro.pages) {
        $nm = $pg.name; $nm2 = $pg.Name
        $cap = $pg.caption; $vt = $pg.viewType
        $compCount = 0
        if ($pg.components) { $compCount = $pg.components.Count }
        $sz = ($pg | ConvertTo-Json -Depth 20 -Compress).Length
        Write-Host ("  page name={0} caption={1} viewType={2} components={3} jsonSize={4} keys=[{5}]" -f $nm, $cap, $vt, $compCount, $sz, (($pg.PSObject.Properties.Name) -join ','))
      }
    } else {
      Write-Host ('  pages is NOT array: ' + $ro.pages.GetType().Name + ' keys=[' + (($ro.pages.PSObject.Properties.Name | Select-Object -First 20) -join ',') + ']')
    }
  }

  # layout detail
  if ($ro.layout) {
    $lsz = ($ro.layout | ConvertTo-Json -Depth 20 -Compress).Length
    Write-Host ("`nlayout: type={0} jsonSize={1} keys=[{2}]" -f $ro.layout.GetType().Name, $lsz, (($ro.layout.PSObject.Properties.Name | Select-Object -First 20) -join ','))
    if ($ro.layout.pages) { Write-Host ('  layout.pages type=' + $ro.layout.pages.GetType().Name) }
  }

  # events/tools/dataviews/rules lengths
  foreach ($k in @('events','tools','dataviews','rules')) {
    if ($ro.PSObject.Properties.Name -contains $k -and $ro.$k) {
      $v = $ro.$k
      if ($v -is [System.Array]) {
        $sz = ($v | ConvertTo-Json -Depth 20 -Compress).Length
        Write-Host ("{0}: array count={1} jsonSize={2}" -f $k, $v.Count, $sz)
      } else {
        Write-Host ("{0}: type={1}" -f $k, $v.GetType().Name)
      }
    }
  }
}

Analyze 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\fullapp_v4_expanded.json' 'V4-EXPANDED (dsTrans+TransferPrice V4)'
Analyze 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe\getapp_case2.json' 'TEST-A BASE'
