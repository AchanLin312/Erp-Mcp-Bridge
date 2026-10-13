# Extract App Studio designer property-panel schema from Kinetic main.js
# Output: docs/designer-property-schema.json
$ErrorActionPreference = 'Stop'

$jsPath  = 'D:\Epicor\Server.Latest\Apps\ERP\Home\main.be1d1e62c94b7406.js'
$outPath = 'D:\MCP\Erp-Mcp-Bridge\docs\designer-property-schema.json'

$js = Get-Content $jsPath -Raw
Write-Host ("Loaded chars: " + $js.Length)

function Get-Blk([string]$txt, [int]$pos) {
    # txt[pos] must be an opener: ( or { or [
    $stk = New-Object System.Collections.Stack
    $q = $null
    for ($i = $pos; $i -lt $txt.Length; $i++) {
        $ch = $txt[$i]
        if ($q) {
            if ($ch -eq '\') { $i++; continue }
            if ($ch -eq $q) { $q = $null }
            continue
        }
        if ($ch -eq '"') { $q = '"' }
        elseif ($ch -eq "'") { $q = "'" }
        elseif ($ch -eq '(') { [void]$stk.Push(')') }
        elseif ($ch -eq '{') { [void]$stk.Push('}') }
        elseif ($ch -eq '[') { [void]$stk.Push(']') }
        elseif ($ch -eq ')') { if ($stk.Count -gt 0 -and $stk.Peek() -eq ')') { [void]$stk.Pop(); if ($stk.Count -eq 0) { return $txt.Substring($pos, $i - $pos + 1) } } }
        elseif ($ch -eq '}') { if ($stk.Count -gt 0 -and $stk.Peek() -eq '}') { [void]$stk.Pop(); if ($stk.Count -eq 0) { return $txt.Substring($pos, $i - $pos + 1) } } }
        elseif ($ch -eq ']') { if ($stk.Count -gt 0 -and $stk.Peek() -eq ']') { [void]$stk.Pop(); if ($stk.Count -eq 0) { return $txt.Substring($pos, $i - $pos + 1) } } }
    }
    return $null
}

# 1) editorType enum (Bi)
$edTypes = [ordered]@{}
$m = [regex]::Match($js, '\w+=function\(i\)\{return i\[i\.auto=0\]="auto"[^}]*\}')
if ($m.Success) {
    foreach ($e in [regex]::Matches($m.Value, 'i\[i\.(\w+)=(\d+)\]="(\w+)"')) {
        $edTypes[$e.Groups[1].Value] = [int]$e.Groups[2].Value
    }
}
Write-Host ("editorTypes: " + $edTypes.Count)

# 2) webpack export alias map
$al = @{}
foreach ($m2 in [regex]::Matches($js, '(\w{2,5}):\(\)=>(\w{2,5})')) {
    $k = $m2.Groups[1].Value
    if (-not $al.ContainsKey($k)) { $al[$k] = $m2.Groups[2].Value }
}
Write-Host ("aliasMap: " + $al.Count)

# 3) enum value resolver
$ec = @{}
function Get-Enum([string]$ref) {
    if ($ec.ContainsKey($ref)) { return $ec[$ref] }
    $script:ec[$ref] = $null
    $loc = $ref -replace '^.*\.', ''
    $pat = [regex]::Escape($loc) + '=function\(i\)\{return (.{10,3000}?)\}\(' + [regex]::Escape($loc)
    $mm = [regex]::Match($script:js, $pat)
    if (-not $mm.Success) { return $null }
    $body = $mm.Groups[1].Value
    $vals = [ordered]@{}
    foreach ($e in [regex]::Matches($body, 'i\[i\.(\w+)=(\d+)\]="([^"]+)"')) {
        $vals[$e.Groups[3].Value] = [int]$e.Groups[2].Value
    }
    foreach ($e in [regex]::Matches($body, 'i\.(\w+)="([^"]*)"')) {
        $v = $e.Groups[2].Value
        if (-not $vals.Contains($v)) { $vals[$v] = $e.Groups[1].Value }
    }
    if ($vals.Count -gt 0) { $script:ec[$ref] = $vals; return $vals }
    return $null
}

# 4) class definition resolver
$cc = @{}
function Get-Cls([string]$name) {
    if ($cc.ContainsKey($name)) { return $cc[$name] }
    $script:cc[$name] = $null
    $idx = $script:js.IndexOf('class ' + $name + ' ')
    if ($idx -lt 0) { $idx = $script:js.IndexOf('class ' + $name + '{') }
    if ($idx -lt 0) { return $null }
    $head = $script:js.Substring($idx, [Math]::Min(300, $script:js.Length - $idx))
    $em = [regex]::Match($head, 'class \w+(?:\s+extends\s+(\w+))?')
    $b = $null
    if ($em.Success -and $em.Groups[1].Success) { $b = $em.Groups[1].Value }
    $br = $script:js.IndexOf('{', $idx)
    if ($br -lt 0) { return $null }
    $def = @{ base = $b; body = (Get-Blk $script:js $br) }
    $script:cc[$name] = $def
    return $def
}

# 5) property groups inside a class body
function Get-Grps([string]$cbody) {
    $grps = [ordered]@{}
    if (-not $cbody) { return $grps }
    foreach ($g in [regex]::Matches($cbody, 'this\.(\w*(?:[Pp]roperties|propertyModel))\s*=\s*')) {
        $nm = $g.Groups[1].Value
        if ($grps.Contains($nm)) { continue }
        $br = $g.Index + $g.Length
        if ($br -lt $cbody.Length -and $cbody[$br] -eq '{') {
            $blk = Get-Blk $cbody $br
            if ($blk) { $grps[$nm] = $blk }
        }
    }
    return $grps
}

# 6) parse one entry value object
function Parse-Ent([string]$etext) {
    $o = [ordered]@{}
    $et = [regex]::Match($etext, 'editorType:(\w+)\.(\w+)')
    if ($et.Success) { $o['editorType'] = $et.Groups[2].Value }
    $en = [regex]::Match($etext, ',enum:(\w+)')
    if (-not $en.Success) { $en = [regex]::Match($etext, '\{enum:(\w+)') }
    if ($en.Success) {
        $o['enumRef'] = $en.Groups[1].Value
        $vals = Get-Enum $en.Groups[1].Value
        if ($vals) { $o['enum'] = $vals }
    }
    $cg = [regex]::Match($etext, 'category:this\.(\w+)\.(\w+)')
    if ($cg.Success) { $o['category'] = $cg.Groups[2].Value }
    $hd = [regex]::Match($etext, 'hidden:!(0|1)')
    if ($hd.Success) { $o['hidden'] = ($hd.Groups[1].Value -eq '0') }
    $pz = [regex]::Match($etext, 'isPrismProperty:!(0|1)')
    if ($pz.Success) { $o['isPrismProperty'] = ($pz.Groups[1].Value -eq '0') }
    return $o
}

function Parse-Grp([string]$gtext) {
    $res = [ordered]@{}
    if (-not $gtext) { return $res }
    foreach ($p in [regex]::Matches($gtext, '(?:^|[,{])(\w+):\{')) {
        $key = $p.Groups[1].Value
        if ($res.Contains($key)) { continue }
        $br = $p.Index + $p.Length - 1
        $blk = Get-Blk $gtext $br
        if ($blk) { $res[$key] = Parse-Ent $blk }
    }
    return $res
}

# 7) build inheritance chain for a properties class ref
function Build-Chain([string]$ref) {
    $loc = $ref -replace '^.*\.', ''
    if ($al.ContainsKey($loc)) { $loc = $al[$loc] }
    $chain = @()
    $cur = $loc; $depth = 0
    while ($cur -and $depth -lt 6) {
        $def = Get-Cls $cur
        if (-not $def) { break }
        $grps = Get-Grps $def.body
        $pg = [ordered]@{}
        foreach ($gk in $grps.Keys) { $pg[$gk] = Parse-Grp $grps[$gk] }
        $chain += ,@{ className = $cur; base = $def.base; groups = $pg }
        if (-not $def.base) { break }
        $cur = $def.base
        $depth++
    }
    return @{ class = $loc; chain = $chain }
}

# 8) scan registerComponent calls
$comps = [ordered]@{}
foreach ($m3 in [regex]::Matches($js, 'registerComponent\("([\w-]+)"')) {
    $cid = $m3.Groups[1].Value
    if ($comps.Contains($cid)) { continue }
    $op = $m3.Index + 'registerComponent'.Length
    $blk = Get-Blk $js $op
    if (-not $blk) { Write-Host ("  UNBALANCED: " + $cid); continue }
    $rec = [ordered]@{ source = 'main.js' }
    $ov = [regex]::Match($blk, 'override:"([^"]+)"')
    if ($ov.Success) { $rec['override'] = $ov.Groups[1].Value }
    $ip = [regex]::Match($blk, 'imagePath:"([^"]+)"')
    if ($ip.Success) { $rec['imagePath'] = $ip.Groups[1].Value }
    $dn = [regex]::Match($blk, 'displayName:this\.strings\.(\w+)')
    if ($dn.Success) { $rec['displayNameRef'] = $dn.Groups[1].Value }
    $bh = [regex]::Match($blk, 'behavior:\{')
    if ($bh.Success) {
        $bblk = Get-Blk $blk ($bh.Index + 'behavior:'.Length)
        if ($bblk) {
            $bj = $bblk -replace '!0', 'true' -replace '!1', 'false'
            $bj = [regex]::Replace($bj, '(?<=[{,])(\w+):', '"$1":')
            $rec['behavior'] = $bj
        }
    }
    $pr = [regex]::Match($blk, 'properties:(?:new )([\w.]+)')
    if ($pr.Success) {
        $built = Build-Chain $pr.Groups[1].Value
        $rec['propertiesClass'] = $built.class
        $rec['propertyChain'] = $built.chain
    }
    $comps[$cid] = $rec
}
Write-Host ("registerComponent parsed: " + $comps.Count)

# 9) componentPropertySvc.add(...) registrations
foreach ($m4 in [regex]::Matches($js, 'componentPropertySvc\.add\("([\w-]+)"')) {
    $cid = $m4.Groups[1].Value
    if ($comps.Contains($cid)) { continue }
    $op = $m4.Index + $m4.Value.Length
    $sb = $js.IndexOf('{', $op)
    if ($sb -lt 0) { continue }
    $blk = Get-Blk $js $sb
    if (-not $blk) { continue }
    $rec = [ordered]@{ source = 'main.js (componentPropertySvc.add)' }
    $ip = [regex]::Match($blk, 'imagePath:"([^"]+)"')
    if ($ip.Success) { $rec['imagePath'] = $ip.Groups[1].Value }
    $dn = [regex]::Match($blk, 'displayName:this\.strings\.(\w+)')
    if ($dn.Success) { $rec['displayNameRef'] = $dn.Groups[1].Value }
    $pr = [regex]::Match($blk, 'properties:(?:new )([\w.]+)')
    if ($pr.Success) {
        $built = Build-Chain $pr.Groups[1].Value
        $rec['propertiesClass'] = $built.class
        $rec['propertyChain'] = $built.chain
    }
    $comps[$cid] = $rec
}
Write-Host ("after svc.add: " + $comps.Count)

# 10) emit
$doc = [ordered]@{
    meta = [ordered]@{
        source      = 'main.be1d1e62c94b7406.js'
        fileSize    = $js.Length
        extractedAt = (Get-Date -Format 'yyyy-MM-dd')
        extractor   = 'extract-designer-schema.ps1 v2'
        editorTypes = $edTypes
        notes       = 'displayName/description/labelText/category are localization refs. Enums resolved where found. propertyChain: child class first, runtime merges groups in chain order.'
    }
    components = $comps
}

$json = $doc | ConvertTo-Json -Depth 24
[System.IO.File]::WriteAllText($outPath, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("written: " + $outPath + " (" + $json.Length + " chars)")
