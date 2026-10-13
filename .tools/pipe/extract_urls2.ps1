# Scan multiple DLLs for URL-related literals
$files = @(
  'c:\Users\ssi-LinYaoChen\.nuget\packages\epicorrestapi\3.0.2\lib\net48\EpicorRestAPI.dll',
  'd:\MCP\Erp-Mcp-Bridge\publish\EpicorRestAPI.dll'
)
$pats = @('api/v', 'ODataUri', 'MethodUri', 'odata')
foreach ($f in $files) {
  Write-Host "######## $f"
  $bytes = [IO.File]::ReadAllBytes($f)
  $us = [Text.Encoding]::Unicode.GetString($bytes)
  $as = [Text.Encoding]::ASCII.GetString($bytes)
  foreach ($p in $pats) {
    foreach ($encName in @('ASCII', 'Unicode')) {
      $txt = if ($encName -eq 'ASCII') { $as } else { $us }
      $i = 0; $found = $false
      while (($i = $txt.IndexOf($p, $i)) -ge 0) {
        $s = [Math]::Max(0, $i - 60); $ln = [Math]::Min(160, $txt.Length - $s)
        $seg = $txt.Substring($s, $ln) -replace '[^\x20-\x7E]', '.'
        Write-Host ("  [{0}/{1}] {2}" -f $encName, $p, $seg)
        $i++; $found = $true
      }
    }
  }
}
