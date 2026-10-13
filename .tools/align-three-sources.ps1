# align-three-sources.ps1  (ASCII only - PS 5.1 GBK safe)
# Aligns decompiled DLL [ComponentModel] types with designer property panel schema.
# Input : .decomp/MetaFXCore/**/*.cs , docs/designer-property-schema.json
# Output: docs/three-source-alignment.json

$ErrorActionPreference = 'Stop'
$root = 'd:\MCP\Erp-Mcp-Bridge'
$decomp = Join-Path $root '.decomp\MetaFXCore'
$designerPath = Join-Path $root 'docs\designer-property-schema.json'
$outPath = Join-Path $root 'docs\three-source-alignment.json'

function Camelize([string]$s) {
    if ([string]::IsNullOrEmpty($s) -or $s.Length -eq 1) { return $s.ToLower() }
    return $s.Substring(0, 1).ToLower() + $s.Substring(1)
}

# ---------- Step 1: parse every class in decompiled Models tree ----------
$attrRx = [regex]'\[ComponentModel\("([^"]+)"\)\]'
$clsRx  = [regex]'class\s+(\w+)\s*(?::\s*([\w.]+))?'
$propRx = [regex]'public\s+(?:static\s+)?(?<type>[\w<>\[\],.?\s]+?)\s+(?<name>\w+)\s*\{\s*get;'
$jpRx   = [regex]'JsonProperty\s*\((?<args>[^\)]*)\)'
$pnRx   = [regex]'PropertyName\s*=\s*"([^"]+)"'

$classMap = @{}
$files = Get-ChildItem $decomp -Recurse -Filter *.cs
foreach ($f in $files) {
    $txt = [System.IO.File]::ReadAllText($f.FullName)
    if ($txt -match '\binterface\s+') { continue }
    $clsM = $clsRx.Match($txt)
    if (-not $clsM.Success) { continue }
    $tid = ''
    $attrM = $attrRx.Match($txt)
    if ($attrM.Success) { $tid = $attrM.Groups[1].Value }
    $folder = $f.Directory.Name

    $props = [ordered]@{}
    $pending = New-Object System.Collections.Generic.List[string]
    foreach ($line in ($txt -split "`n")) {
        $t = $line.Trim()
        if ($t.StartsWith('[')) { $pending.Add($t); continue }
        if ($t -eq '' -or $t -eq '{' -or $t -eq '}' -or $t.StartsWith('//')) { continue }
        $m = $propRx.Match($line)
        if ($m.Success) {
            $name = $m.Groups['name'].Value
            $jsonName = Camelize $name
            foreach ($a in $pending) {
                $jm = $jpRx.Match($a)
                if ($jm.Success) {
                    $pm = $pnRx.Match($jm.Groups['args'].Value)
                    if ($pm.Success) { $jsonName = $pm.Groups[1].Value }
                }
            }
            if (-not $props.Contains($jsonName)) {
                $props[$jsonName] = @{ csName = $name; type = $m.Groups['type'].Value.Trim() }
            }
        }
        $pending.Clear()
    }
    if (-not $classMap.ContainsKey($clsM.Groups[1].Value)) {
        $classMap[$clsM.Groups[1].Value] = @{ file = $f.Name; folder = $folder; base = ''; tid = $tid; props = $props }
        if ($clsM.Groups[2].Success) { $classMap[$clsM.Groups[1].Value].base = $clsM.Groups[2].Value }
    }
}
Write-Host ("classes parsed: " + $classMap.Count)

# ---------- Step 2: resolve full inheritance chain -> flat json property set ----------
function Get-FlatProps([string]$cls) {
    $res = [ordered]@{}
    $seen = @{}
    $c = $cls
    $depth = 0
    while ($c -and $depth -lt 10) {
        if (-not $classMap.ContainsKey($c)) { break }
        $e = $classMap[$c]
        foreach ($k in $e.props.Keys) {
            if (-not $seen.ContainsKey($k)) {
                $seen[$k] = $true
                $res[$k] = @{ type = $e.props[$k].type; from = $c }
            }
        }
        $b = $e.base
        if (-not $b) { break }
        $c = ($b -split '\.')[-1]
        $depth++
    }
    return $res
}

$tids = [ordered]@{}
foreach ($cls in $classMap.Keys) {
    $e = $classMap[$cls]
    if ($e.tid -and -not $tids.Contains($e.tid)) { $tids[$e.tid] = $cls }
}
Write-Host ("dll componentModel registrations: " + $tids.Count)

# ---------- Step 3: load designer schema and flatten property chains ----------
$des = Get-Content $designerPath -Raw | ConvertFrom-Json
$desFlat = @{}
$desChain = @{}
foreach ($p in $des.components.PSObject.Properties) {
    $cid = $p.Name; $c = $p.Value
    $flat = [ordered]@{}
    if ($c.propertyChain) {
        foreach ($ch in $c.propertyChain) {
            if (-not $ch.groups) { continue }
            foreach ($g in $ch.groups.PSObject.Properties) {
                foreach ($pr in $g.Value.PSObject.Properties) {
                    if (-not $flat.Contains($pr.Name)) {
                        $flat[$pr.Name] = @{ editorType = $pr.Value.editorType; group = $g.Name; cls = $ch.className }
                    }
                }
            }
        }
    }
    $desFlat[$cid] = $flat
    $chainNames = @()
    if ($c.propertyChain) { foreach ($ch in $c.propertyChain) { $chainNames += $ch.className } }
    $desChain[$cid] = ($chainNames -join ' > ')
}
Write-Host ("designer components: " + $desFlat.Count)

# resolve designer-side inheritance: erp-* components override ep-* panels,
# own chain groups may be empty -> merge override component's props (own wins)
$desOverride = @{}
foreach ($p in $des.components.PSObject.Properties) {
    if ($p.Value.override) { $desOverride[$p.Name] = $p.Value.override }
}
function Build-EffFlat([string]$cid, [int]$depth) {
    $eff = [ordered]@{}
    if ($depth -lt 6 -and $desOverride.ContainsKey($cid) -and $desFlat.ContainsKey($desOverride[$cid])) {
        $par = Build-EffFlat $desOverride[$cid] ($depth + 1)
        foreach ($k in $par.Keys) { if (-not $eff.Contains($k)) { $eff[$k] = $par[$k] } }
    }
    foreach ($k in $desFlat[$cid].Keys) { if (-not $eff.Contains($k)) { $eff[$k] = $desFlat[$cid][$k] } }
    return $eff
}
$desEff = @{}
foreach ($cid in $desFlat.Keys) { $desEff[$cid] = Build-EffFlat $cid 0 }
$effCounts = @(); foreach ($cid in $desEff.Keys) { $effCounts += "$cid=" + $desEff[$cid].Count }
Write-Host ("effective designer props: " + (($effCounts | Sort-Object) -join ', '))

# ---------- Step 4: id-level alignment ----------
$both = @(); $dllOnly = @(); $desOnly = @(); $cfgOnly = @()
foreach ($tid in $tids.Keys) {
    if ($tid.StartsWith('cfg-')) { $cfgOnly += $tid; continue }
    if ($desFlat.ContainsKey($tid)) { $both += $tid } else { $dllOnly += $tid }
}
foreach ($cid in $desFlat.Keys) { if (-not $tids.Contains($cid)) { $desOnly += $cid } }
$both = @($both | Sort-Object); $dllOnly = @($dllOnly | Sort-Object); $desOnly = @($desOnly | Sort-Object); $cfgOnly = @($cfgOnly | Sort-Object)
Write-Host ("both: " + $both.Count + " / dllOnly(appStudio): " + $dllOnly.Count + " / designerOnly: " + $desOnly.Count + " / cfgOnly: " + $cfgOnly.Count)

# ---------- Step 5: property-level alignment for 'both' + base class check ----------
$propLevel = [ordered]@{}
$sharedTotal = 0; $dllSideTotal = 0; $desSideTotal = 0
foreach ($tid in $both) {
    $dllProps = Get-FlatProps $tids[$tid]
    $d = $desEff[$tid]
    $shared = @(); $dllSide = @(); $desSide = @()
    foreach ($k in $dllProps.Keys) { if ($d.Contains($k)) { $shared += $k } else { $dllSide += $k } }
    foreach ($k in $d.Keys) { if (-not $dllProps.Contains($k)) { $desSide += $k } }
    $sharedTotal += $shared.Count; $dllSideTotal += $dllSide.Count; $desSideTotal += $desSide.Count
    $propLevel[$tid] = [ordered]@{
        dllClass      = $tids[$tid]
        shared        = $shared
        dllOnly       = $dllSide
        designerOnly  = $desSide
        dllPropertyCount = $dllProps.Count
        designerPropertyCount = $d.Count
    }
}

# base class check via frequency: props present in >= N of 65 designer components
# (main.js obfuscated base class g5 carries no propertySvc groups, so panel-level
#  "base-like" props are the ones shared across nearly all components)
$freq = @{}
foreach ($cid in $desEff.Keys) {
    foreach ($k in $desEff[$cid].Keys) {
        if ($freq.ContainsKey($k)) { $freq[$k]++ } else { $freq[$k] = 1 }
    }
}
$threshold = 10
$commonProps = @()
foreach ($k in $freq.Keys) { if ($freq[$k] -ge $threshold) { $commonProps += $k } }
$commonProps = @($commonProps | Sort-Object)
$allFreq = [ordered]@{}
$freq.Keys | Sort-Object { -$freq[$_] } | ForEach-Object { $allFreq[$_] = $freq[$_] }
$dllBase = Get-FlatProps 'ComponentModel'
$bShared = @(); $bDllOnly = @(); $bDesOnly = @()
foreach ($k in $dllBase.Keys) { if ($commonProps -contains $k) { $bShared += $k } else { $bDllOnly += $k } }
foreach ($k in $commonProps) { if (-not $dllBase.Contains($k)) { $bDesOnly += $k } }
$baseInfo = [ordered]@{
    method             = 'frequency'
    designerComponents = $desFlat.Count
    commonThreshold    = $threshold
    designerCommonPropCount = $commonProps.Count
    dllBaseClass       = 'ComponentModel'
    dllBasePropCount   = $dllBase.Count
    shared             = $bShared
    dllOnly            = $bDllOnly
    designerOnly       = $bDesOnly
    designerCommonPropsWithFreq = [ordered]@{}
    allPropsByFreq     = $allFreq
}
foreach ($k in $commonProps) { $baseInfo.designerCommonPropsWithFreq[$k] = $freq[$k] }

# ---------- Step 6: designer-only inventory (for report + corpus check) ----------
$desOnlyInv = [ordered]@{}
foreach ($cid in $desOnly) {
    $desOnlyInv[$cid] = [ordered]@{
        propertyCount = $desEff[$cid].Count
        chain = $desChain[$cid]
    }
}

# ---------- Step 7: write output ----------
$dllTypesOut = [ordered]@{}
foreach ($tid in $tids.Keys) {
    $fp = Get-FlatProps $tids[$tid]
    $dllTypesOut[$tid] = [ordered]@{ cls = $tids[$tid]; jsonProps = @($fp.Keys) }
}

$out = [ordered]@{
    meta = [ordered]@{
        generatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        dllSource   = 'Epicor.MetaFX.Core.dll (decompiled -> .decomp/MetaFXCore)'
        designerSource = 'main.be1d1e62c94b7406.js (via .tools/extract-designer-schema.ps1 -> docs/designer-property-schema.json)'
    }
    summary = [ordered]@{
        dllRegistrations = $tids.Count
        dllAppStudio     = ($both.Count + $dllOnly.Count)
        dllConfigurator  = $cfgOnly.Count
        designerComponents = $desFlat.Count
        both             = $both.Count
        dllOnly          = $dllOnly
        designerOnly     = $desOnly
        configuratorOnly = $cfgOnly
        propertyLevelTotals = [ordered]@{ shared = $sharedTotal; dllOnly = $dllSideTotal; designerOnly = $desSideTotal }
    }
    baseClassCheck = $baseInfo
    bothTypes = $both
    propertyLevel = $propLevel
    designerOnlyInventory = $desOnlyInv
    dllTypes = $dllTypesOut
}
$json = $out | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($outPath, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("written: " + $outPath + " (" + $json.Length + " chars)")
Write-Host ("base check shared: " + $baseInfo.shared.Count + " dllOnly: " + $baseInfo.dllOnly.Count + " designerOnly: " + $baseInfo.designerOnly.Count)
