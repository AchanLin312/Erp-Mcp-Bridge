# Extract HEAD snippets of other layers to compare top-level structure
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$cs = 'Data Source=192.6.4.16;Initial Catalog=GPEDev;User ID=epicorread;Password=epicorread;TrustServerCertificate=true;Connect Timeout=30'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()

$samples = @(
  @{ Key1 = 'Part Inquiry';        Key2 = 'Ice.UIDbd.PartInquiry_Standard' },
  @{ Key1 = 'TransferPrice_V5';    Key2 = 'Ice.UIDbd.dsTransPriceUpdate' },
  @{ Key1 = 'WFSetting';           Key2 = 'Ice.UI.UD110Entry' },
  @{ Key1 = 'SOSearch';            Key2 = 'Ice.UIDbd.GPSalesOrderSearch' }
)

foreach ($s in $samples) {
  $sql = "SELECT TOP 1 Version, Content FROM Ice.UXAppVersion WITH (NOLOCK) WHERE TypeCode='KNTCCustLayer' AND Key1=@k1 AND Key2=@k2 AND Key3='Desktop' ORDER BY Version DESC"
  $cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
  $cmd.Parameters.AddWithValue('@k1', $s.Key1) | Out-Null
  $cmd.Parameters.AddWithValue('@k2', $s.Key2) | Out-Null
  $rdr = $cmd.ExecuteReader()
  if ($rdr.Read()) {
    $c = $rdr['Content']
    Write-Output ("=== {0} v{1} len={2}" -f $s.Key1, $rdr['Version'], $c.Length)
    Write-Output ("HEAD: {0}" -f $c.Substring(0, [Math]::Min(700, $c.Length)))
    # top-level keys via quick regex on first-level
    try {
      $j = $c | ConvertFrom-Json
      Write-Output ("TOPKEYS: {0}" -f (($j.PSObject.Properties | ForEach-Object { $_.Name }) -join ', '))
      if ($j.pages) { Write-Output ("PAGES count: {0}" -f $j.pages.Count) }
    } catch { Write-Output ("JSON INVALID: {0}" -f $_.Exception.Message) }
    Write-Output ''
  } else {
    Write-Output ("=== {0}: NO ROW" -f $s.Key1)
  }
  $rdr.Close()
}
$conn.Close()
