# Extract TransferPrice V4 damaged layer content from SQL to local file (no LLM context)
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'

$cs = 'Data Source=192.6.4.16;Initial Catalog=GPEDev;User ID=epicorread;Password=epicorread;TrustServerCertificate=true;Connect Timeout=30'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()

# 1) list all versions metadata
$metaSql = @"
SELECT Company, TypeCode, Key1, Key2, Key3, Version, Comment, CreatedBy, CreatedOn,
       LEN(Content) AS ContentLen, SysRowID
FROM Ice.UXAppVersion WITH (NOLOCK)
WHERE Key1 = 'TransferPrice V4' AND Key2 = 'Ice.UIDbd.dsTransPriceUpdate'
ORDER BY TypeCode, Version DESC
"@
$cmd = New-Object System.Data.SqlClient.SqlCommand($metaSql, $conn)
$rdr = $cmd.ExecuteReader()
$meta = New-Object System.Collections.Generic.List[object]
while ($rdr.Read()) {
  $meta.Add([pscustomobject]@{
    Company = $rdr['Company']; TypeCode = $rdr['TypeCode']; Key3 = $rdr['Key3']
    Version = $rdr['Version']; Comment = $rdr['Comment']; CreatedBy = $rdr['CreatedBy']
    CreatedOn = $rdr['CreatedOn']; ContentLen = $rdr['ContentLen']; SysRowID = $rdr['SysRowID']
  })
}
$rdr.Close()
$meta | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $dir 'transferprice_meta.json') -Encoding UTF8
Write-Output ("META rows: {0}" -f $meta.Count)
$meta | ForEach-Object { Write-Output ("  {0} v{1} {2} len={3} {4} {5}" -f $_.TypeCode, $_.Version, $_.Key3, $_.ContentLen, $_.CreatedBy, $_.CreatedOn) }

# 2) extract latest KNTCCustLayer GPEHZ content (the damaged customer layer)
$sql = @"
SELECT TOP 1 Company, Content
FROM Ice.UXAppVersion WITH (NOLOCK)
WHERE Key1 = 'TransferPrice V4' AND Key2 = 'Ice.UIDbd.dsTransPriceUpdate'
  AND TypeCode = 'KNTCCustLayer' AND Key3 = 'Desktop'
ORDER BY Version DESC
"@
$cmd2 = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
$cmd2.CommandTimeout = 120
$rdr2 = $cmd2.ExecuteReader()
if (-not $rdr2.Read()) { throw 'no content row found' }
$comp = $rdr2['Company']
$content = $rdr2['Content']
$rdr2.Close()
$conn.Close()

if (-not $content) { throw 'content is null' }
$rawPath = Join-Path $dir 'transferprice_v4_raw.json'
[System.IO.File]::WriteAllText($rawPath, $content, [System.Text.Encoding]::UTF8)
Write-Output ("RAW extracted: company=[{0}] {1} chars -> {2}" -f $comp, $content.Length, $rawPath)
Write-Output ("HEAD 400: {0}" -f $content.Substring(0, [Math]::Min(400, $content.Length)))
