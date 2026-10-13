# Analyze damaged layer content: structure, duplicate/deleted ops stats
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$rawPath = Join-Path $dir 'transferprice_v4_raw.json'
$raw = [System.IO.File]::ReadAllText($rawPath)

# 1) JSON validity
try {
  $j = $raw | ConvertFrom-Json -ErrorAction Stop
  Write-Output "JSON: VALID"
} catch {
  Write-Output ("JSON: INVALID -> {0}" -f $_.Exception.Message)
  exit 1
}

# 2) top-level keys
$props = $j.PSObject.Properties
Write-Output ("TOP-LEVEL KEYS: {0}" -f (($props | ForEach-Object { $_.Name }) -join ', '))

# 3) per-key analysis: find Diffs arrays and count operations
function Analyze-Diffs($name, $node) {
  if ($null -eq $node) { return }
  if ($node -is [System.Array]) {
    $n = $node.Count
    $ops = @{}
    foreach ($it in $node) {
      $op = $it.Operation
      if (-not $op) { $op = '<none>' }
      if ($ops.ContainsKey($op)) { $ops[$op]++ } else { $ops[$op] = 1 }
    }
    $opStr = ($ops.GetEnumerator() | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value }) -join ', '
    Write-Output ("{0}: ARRAY n={1} ops[{2}]" -f $name, $n, $opStr)
    # duplicate (Id+Path) detection
    $groups = @{}
    foreach ($it in $node) {
      $k = ("{0}|{1}|{2}" -f $it.Id, $it.Path, $it.Index)
      if ($groups.ContainsKey($k)) { $groups[$k]++ } else { $groups[$k] = 1 }
    }
    $dups = $groups.GetEnumerator() | Where-Object { $_.Value -gt 1 } | Sort-Object Value -Descending
    $dupTotal = ($dups | Measure-Object).Count
    $dupExtra = ($dups | Measure-Object -Property Value -Sum).Sum - $dupTotal
    Write-Output ("  dup keys: {0} keys with repeats, {1} redundant items" -f $dupTotal, $dupExtra)
    $dups | Select-Object -First 5 | ForEach-Object { Write-Output ("  TOP-DUP x{0}: {1}" -f $_.Value, $_.Key) }
  } elseif ($node -is [System.Management.Automation.PSCustomObject]) {
    # container object e.g. layout = {Name, Version, Diffs}
    $names = $node.PSObject.Properties.Name
    Write-Output ("{0}: OBJECT keys[{1}]" -f $name, ($names -join ','))
    foreach ($p in $node.PSObject.Properties) {
      if ($p.Name -eq 'Diffs' -or $p.Value -is [System.Array]) {
        Analyze-Diffs ("{0}.{1}" -f $name, $p.Name) $p.Value
      }
    }
  } else {
    $s = "$node"
    if ($s.Length -gt 80) { $s = $s.Substring(0,80) + '...' }
    Write-Output ("{0}: SCALAR = {1}" -f $name, $s)
  }
}

foreach ($p in $props) {
  Analyze-Diffs $p.Name $p.Value
}
