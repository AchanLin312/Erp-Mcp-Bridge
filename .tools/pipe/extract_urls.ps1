# Extract URL-related string literals from EpicorRESTAPICore.dll
$bytes = [IO.File]::ReadAllBytes('c:\Users\ssi-LinYaoChen\.nuget\packages\epicorrestapicore\3.0.2\lib\net6.0\EpicorRESTAPICore.dll')
$us = [Text.Encoding]::Unicode.GetString($bytes)
$as = [Text.Encoding]::ASCII.GetString($bytes)
$pats = @('api/v1', 'api/v2', '/odata', 'odata/')
foreach ($p in $pats) {
  Write-Host "== ASCII: $p"
  $i = 0
  while (($i = $as.IndexOf($p, $i)) -ge 0) {
    $s = [Math]::Max(0, $i - 50); $ln = [Math]::Min(140, $as.Length - $s)
    $seg = $as.Substring($s, $ln) -replace '[^\x20-\x7E]', '.'
    Write-Host ("  " + $seg)
    $i++
  }
  Write-Host "== Unicode: $p"
  $i = 0
  while (($i = $us.IndexOf($p, $i)) -ge 0) {
    $s = [Math]::Max(0, $i - 50); $ln = [Math]::Min(140, $us.Length - $s)
    $seg = $us.Substring($s, $ln) -replace '[^\x20-\x7E]', '.'
    Write-Host ("  " + $seg)
    $i++
  }
}
