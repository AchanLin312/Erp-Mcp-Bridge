# export-component-schema.ps1  (ASCII only - PS 5.1 GBK safe)
# Merges three-source alignment + designer property panel + presence counts
# into the runtime knowledge base consumed by MetaFxSchemaService / validation.
# Input : docs/three-source-alignment.json     (dllTypes, summary, presence aggregate)
#         docs/designer-property-schema.json   (propertyChain groups, override, editorTypes)
#         .tools/presence-counts.json          (per-typeId file counts)
#         .decomp/MetaFXCore/**/*.cs           (re-parsed for .NET property types)
# Output: MCP/Resources/metafx-component-schema.json

$ErrorActionPreference = 'Stop'
$root         = 'd:\MCP\Erp-Mcp-Bridge'
$alignPath    = Join-Path $root 'docs\three-source-alignment.json'
$designerPath = Join-Path $root 'docs\designer-property-schema.json'
$presencePath = Join-Path $root '.tools\presence-counts.json'
$decomp       = Join-Path $root '.decomp\MetaFXCore'
$outDir       = Join-Path $root 'MCP\Resources'
$outPath      = Join-Path $outDir 'metafx-component-schema.json'

$align    = Get-Content $alignPath    -Raw | ConvertFrom-Json
$designer = Get-Content $designerPath -Raw | ConvertFrom-Json
$presence = Get-Content $presencePath -Raw | ConvertFrom-Json

# ---------- editor types (name <-> code) ----------
$editorTypes = [ordered]@{}
$designer.meta.editorTypes.PSObject.Properties | ForEach-Object { $editorTypes[$_.Name] = $_.Value }
$invEditor = @{}
foreach ($k in @($editorTypes.Keys)) { $invEditor[[string]$editorTypes[$k]] = $k }

function Resolve-EditorType([string]$v) {
    if ([string]::IsNullOrEmpty($v)) { return $null }
    if ($editorTypes.Contains($v)) { return $v }
    if ($invEditor.ContainsKey($v)) { return $invEditor[$v] }
    return $null   # unresolved webpack ref (e.g. "HZf") -> drop
}

# ---------- re-parse decompiled sources for .NET property types ----------
function Camelize([string]$s) {
    if ([string]::IsNullOrEmpty($s) -or $s.Length -eq 1) { return $s.ToLower() }
    return $s.Substring(0, 1).ToLower() + $s.Substring(1)
}
$clsRx  = [regex]'class\s+(\w+)\s*(?::\s*([\w.]+))?'
$propRx = [regex]'public\s+(?:static\s+)?(?<type>[\w<>\[\],.?\s]+?)\s+(?<name>\w+)\s*\{\s*get;'
$jpRx   = [regex]'JsonProperty\s*\((?<args>[^\)]*)\)'
$pnRx   = [regex]'PropertyName\s*=\s*"([^"]+)"'

$classMap = @{}
Get-ChildItem $decomp -Recurse -Filter *.cs | ForEach-Object {
    $txt = [System.IO.File]::ReadAllText($_.FullName)
    if ($txt -match '\binterface\s+') { return }
    $m = $clsRx.Match($txt)
    if (-not $m.Success) { return }
    $name = $m.Groups[1].Value
    if ($classMap.ContainsKey($name)) { return }
    $base = ''
    if ($m.Groups[2].Success) { $base = ($m.Groups[2].Value -split '\.')[-1] }
    $props = [ordered]@{}
    $pending = New-Object System.Collections.Generic.List[string]
    foreach ($line in ($txt -split "`n")) {
        $t = $line.Trim()
        if ($t.StartsWith('[')) { $pending.Add($t); continue }
        if ($t -eq '' -or $t -eq '{' -or $t -eq '}' -or $t.StartsWith('//')) { continue }
        $pm = $propRx.Match($line)
        if ($pm.Success) {
            $jsonName = Camelize $pm.Groups['name'].Value
            foreach ($a in $pending) {
                $jm = $jpRx.Match($a)
                if ($jm.Success) {
                    $pn = $pnRx.Match($jm.Groups['args'].Value)
                    if ($pn.Success) { $jsonName = $pn.Groups[1].Value }
                }
            }
            if (-not $props.Contains($jsonName)) { $props[$jsonName] = $pm.Groups['type'].Value.Trim() }
        }
        $pending.Clear()
    }
    $classMap[$name] = @{ base = $base; props = $props }
}

function Get-FlatProps([string]$cls) {
    # root first, child overrides parent -> base props come first, own props keep last value
    $chain = New-Object System.Collections.Generic.List[object]
    $cur = $cls; $guard = 0
    while (-not [string]::IsNullOrEmpty($cur) -and $classMap.ContainsKey($cur) -and $guard -lt 12) {
        $chain.Add($classMap[$cur])
        $cur = $classMap[$cur].base
        $guard++
    }
    $out = [ordered]@{}
    for ($i = $chain.Count - 1; $i -ge 0; $i--) {
        foreach ($p in $chain[$i].props.Keys) { $out[$p] = $chain[$i].props[$p] }
    }
    return ,$out
}
$baseFlat = Get-FlatProps 'ComponentModel'

# ---------- designer property resolver (override-aware, cached) ----------
$designerComps = @{}
$designer.components.PSObject.Properties | ForEach-Object { $designerComps[$_.Name] = $_.Value }

function Get-ChainProps($comp) {
    $out = [ordered]@{}
    foreach ($link in $comp.propertyChain) {
        if (-not $link.PSObject.Properties['groups']) { continue }
        foreach ($g in $link.groups.PSObject.Properties) {
            foreach ($p in $g.Value.PSObject.Properties) {
                if (-not $out.Contains($p.Name)) { $out[$p.Name] = $p.Value }   # chain order: child wins
            }
        }
    }
    return ,$out
}
$designerCache = @{}
function Get-DesignerProps([string]$tid, [System.Collections.Generic.List[string]]$stack) {
    if ($designerCache.ContainsKey($tid)) { return $designerCache[$tid] }
    if (-not $designerComps.ContainsKey($tid)) { return ,([ordered]@{}) }
    if ($stack -contains $tid) { return ,([ordered]@{}) }   # cycle guard
    $stack.Add($tid)
    $own = Get-ChainProps $designerComps[$tid]
    $ov = $designerComps[$tid].PSObject.Properties['override']
    if ($ov -and $ov.Value) {
        $bp = Get-DesignerProps ([string]$ov.Value) $stack
        foreach ($p in $bp.Keys) { if (-not $own.Contains($p)) { $own[$p] = $bp[$p] } }
    }
    $stack.RemoveAt($stack.Count - 1)
    $designerCache[$tid] = $own
    return ,$own
}

# ---------- assemble ----------
$dllTypes = $align.dllTypes
$counts = @{}
$presence.counts.PSObject.Properties | ForEach-Object { $counts[$_.Name] = [int]$_.Value }

$universe = New-Object System.Collections.Generic.List[string]
foreach ($k in $dllTypes.PSObject.Properties.Name) { if (-not $universe.Contains($k)) { $universe.Add($k) } }
foreach ($k in $designerComps.Keys)                 { if (-not $universe.Contains($k)) { $universe.Add($k) } }
foreach ($k in $counts.Keys)                        { if (-not $universe.Contains($k)) { $universe.Add($k) } }

$childCollKnown     = @('components','headerComponents','expandedHeaderComponents','extensionComponents','bodyComponents')
$curatedContainers  = @('metafx-div','metafx-col','metafx-flex-box','metafx-tabstrip','metafx-relationship-map','metafx-matrix','metafx-panel-card-gantt')
$ganttCollections   = @('components','headerComponents','expandedHeaderComponents')

$sorted = @($universe) | ForEach-Object {
    $p = 0
    if ($counts.ContainsKey($_)) { $p = $counts[$_] }
    [pscustomobject]@{ id = $_; presence = $p }
} | Sort-Object -Property @{ Expression = 'presence'; Descending = $true }, @{ Expression = 'id' }

$components = [ordered]@{}
$bySource = [ordered]@{ 'dll+designer' = 0; dllOnly = 0; designerOnly = 0 }
$recList = New-Object System.Collections.Generic.List[string]

foreach ($item in $sorted) {
    $tid = $item.id
    $ns = 'appStudio'
    if ($tid -like 'cfg-*') { $ns = 'configurator' }
    $inDll = $null -ne $dllTypes.PSObject.Properties[$tid]
    $inDes = $designerComps.ContainsKey($tid)
    if ($inDll -and $inDes) { $src = 'dll+designer' } elseif ($inDll) { $src = 'dllOnly' } else { $src = 'designerOnly' }
    $bySource[$src]++

    $cls = $null
    $flat = $null
    $props = [ordered]@{}
    if ($inDll) {
        $cls  = $dllTypes.PSObject.Properties[$tid].Value.cls
        $flat = Get-FlatProps $cls
        foreach ($pn in $flat.Keys) {
            $origin = 'dll'
            if ($baseFlat.Contains($pn)) { $origin = 'base' }
            $props[$pn] = [ordered]@{ origin = $origin; type = [string]$flat[$pn] }
        }
    }
    $dProps = Get-DesignerProps $tid (New-Object System.Collections.Generic.List[string])
    foreach ($pn in $dProps.Keys) {
        $raw = $dProps[$pn]
        $et = $null
        if ($raw.PSObject.Properties['editorType']) { $et = Resolve-EditorType ([string]$raw.editorType) }
        $cat = $null
        if ($raw.PSObject.Properties['category']) { $cat = [string]$raw.category }
        $en = $null
        if ($raw.PSObject.Properties['enum'] -and $raw.enum) { $en = $raw.enum }
        if ($props.Contains($pn)) {
            if ($et)  { $props[$pn].editorType = $et }
            if ($cat) { $props[$pn].category = $cat }
            if ($en)  { $props[$pn].enum = $en }
        } else {
            $e = [ordered]@{ origin = 'designer' }
            if ($et)  { $e.editorType = $et }
            if ($en)  { $e.enum = $en }
            if ($cat) { $e.category = $cat }
            $props[$pn] = $e
        }
    }

    $cc = New-Object System.Collections.Generic.List[string]
    if ($inDll) {
        foreach ($pn in $flat.Keys) {
            if ($childCollKnown -contains $pn -and ([string]$flat[$pn]) -match 'Component') { $cc.Add($pn) }
        }
    }
    if ($tid -eq 'metafx-panel-card-gantt') {
        foreach ($c in $ganttCollections) { if (-not $cc.Contains($c)) { $cc.Add($c) } }
    }
    if ($curatedContainers -contains $tid) {
        if (-not $cc.Contains('components')) { $cc.Add('components') }
    }

    $entry = [ordered]@{}
    $entry.source      = $src
    $entry.namespace   = $ns
    if ($cls) { $entry.cls = $cls }
    $entry.presence    = $item.presence
    $entry.recommended = ($item.presence -gt 0 -and $ns -eq 'appStudio')
    $entry.childCollections = @($cc)
    $entry.properties  = $props
    $components[$tid] = $entry
    if ($entry.recommended) { $recList.Add($tid) }
}

# ---------- preferErp: zero-use ep-* basics with high-use erp-* counterpart ----------
$preferErp = [ordered]@{}
foreach ($tid in $universe) {
    if ($tid -like 'ep-*' -and $counts.ContainsKey($tid) -and $counts[$tid] -eq 0) {
        $alt = 'erp-' + $tid.Substring(3)
        if ($universe.Contains($alt) -and $counts.ContainsKey($alt) -and $counts[$alt] -gt 0) {
            $preferErp[$tid] = $alt
        }
    }
}

# ---------- layout rules (curated from report v2.0 sections 1/3) ----------
$layoutRules = [ordered]@{
    nodeShape = [ordered]@{
        sourceTypeId = '<component type id; MUST be a key of components>'
        model        = '<properties object; keys per components[typeId].properties>'
        components   = '<array of child Component nodes; universal tree slot>'
        boxData      = '<optional grid position {x,y,w,h}>'
    }
    baseProperties = @($baseFlat.Keys)
    boxDataConstraints = [ordered]@{ minX = 1; minY = 1; minW = 6; minH = 3 }
    universalChildSlot = 'children live in node.components (tree form) as full Component nodes'
    panelCardChildCollections = @('components','bodyComponents','expandedHeaderComponents','extensionComponents','data')
    panelCardParentIdConvention = 'flatten: <panelCardId>#<collectionName> (e.g. panel1#bodyComponents)'
    tabstrip = [ordered]@{
        tabs = 'tab definitions in model.data[] items {id,title,page,selected,...}; tab content are child Component nodes'
        parentIdConvention = 'flatten: <tabstripId>~<tabId>'
    }
    contentJsonWarning = 'contentJson must be the INNER layout node (component tree). NEVER pass a file envelope containing $schema/contentVersion/imports.'
    unknownTypeIdBehavior = 'server tolerates unknown sourceTypeId (stored via base model, front-end ignores it) - silent failure. Always pick typeIds from components keys; this file is the whitelist.'
    preferErp = $preferErp
}

$meta = [ordered]@{
    generatedAt    = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    inputs         = @('docs/three-source-alignment.json','docs/designer-property-schema.json','.tools/presence-counts.json','.decomp/MetaFXCore')
    componentCount = $components.Count
    bySource       = $bySource
    editorTypes    = $editorTypes
    recommended    = @($recList)
    notes          = @(
        'property origin: base = ComponentModel base class (19 props); dll = server strong-typed (Epicor.MetaFX.Core.dll [ComponentModel]); designer = App Studio property panel (front-end consumed, server stores verbatim).',
        'dll properties carry .NET type in "type"; designer editorType normalized to editorTypes names, unresolved webpack refs dropped; enum kept verbatim.',
        'cfg-* types belong to the Configurator designer (separate UI), NOT App Studio layouts.',
        'presence = production KNTCMetaUI files containing "sourceTypeId":"<id>" out of 25194 (time snapshot).',
        'recommended = presence > 0 and namespace appStudio.'
    )
}

$out = [ordered]@{ meta = $meta; layoutRules = $layoutRules; components = $components }
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$json = $out | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText($outPath, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("written: {0} ({1:N0} bytes)" -f $outPath, $json.Length)
Write-Host ("components: {0}  bySource: dll+designer={1} dllOnly={2} designerOnly={3}  recommended: {4}" -f `
    $components.Count, $bySource['dll+designer'], $bySource['dllOnly'], $bySource['designerOnly'], $recList.Count)
