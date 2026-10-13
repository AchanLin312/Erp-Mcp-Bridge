# Reflect EpicorRest to get the exact endpoint URL it builds (same params as bridge)
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\bin\Debug\net10.0'
$handler = {
  param($s, $e)
  $p = Join-Path $dir ($e.Name + '.dll')
  if (Test-Path $p) { return [Reflection.Assembly]::LoadFrom($p) }
  return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
$asm = [Reflection.Assembly]::LoadFrom("$dir\EpicorRestAPI.dll")

$enumType = $asm.GetType('EpicorRestAPI.EpicorRestVersion')
$v2 = [Enum]::Parse($enumType, 'V2')

$rest = [Activator]::CreateInstance($asm.GetType('EpicorRestAPI.EpicorRest'))
$rest.AppPoolHost   = 'epicor-srv2019.gpe.com'
$rest.AppPoolInstance = 'gpedev'
$rest.Company       = 'GPEHZ'
$rest.UserName      = 'linyaochen'
$rest.Password      = '15879361481LYC'
$rest.APIKey        = '2zj4lBIO46IrIvaCLHyxAsmuKcr1hiyMi0Se9XBnYSdd7'
$rest.APIVersion    = $v2

Write-Host "== All public string properties:"
foreach ($p in $rest.GetType().GetProperties()) {
  try {
    $v = $p.GetValue($rest)
    if ($v -is [string] -or $v -is [Uri]) { Write-Host ("  {0} = {1}" -f $p.Name, $v) }
  } catch { Write-Host ("  {0} = <err {1}>" -f $p.Name, $_.Exception.Message) }
}

$m = $rest.GetType().GetMethod('BuildMethodEndpointURL', [Reflection.BindingFlags]'NonPublic,Public,Instance')
if ($m) {
  $u1 = $m.Invoke($rest, @('Ice.Lib.MetaFXSvc', 'GetApp'))
  Write-Host ("== BuildMethodEndpointURL(Ice.Lib.MetaFXSvc, GetApp) = {0}" -f $u1)
} else {
  Write-Host "== BuildMethodEndpointURL not found"
}

$b2 = $rest.GetType().GetMethod('BuildBAQEndpointURL', [Reflection.BindingFlags]'NonPublic,Public,Instance')
if ($b2) { Write-Host ("== BuildBAQEndpointURL(Test) = {0}" -f $b2.Invoke($rest, @('Test'))) }

$g = $rest.GetType().GetMethod('GetEnvironment', [Reflection.BindingFlags]'Public,Instance')
Write-Host ("== GetEnvironment exists: {0}" -f ($g -ne $null))
