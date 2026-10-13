# Compare content formats of OTHER layers in DB to establish the standard structure
$ErrorActionPreference = 'Stop'
$dir = 'D:\MCP\Erp-Mcp-Bridge\.tools\pipe'
$cs = 'Data Source=192.6.4.16;Initial Catalog=GPEDev;User ID=epicorread;Password=epicorread;TrustServerCertificate=true;Connect Timeout=30'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()

# 1) distinct layer keys with row counts and sizes
$sql = @"
SELECT TypeCode, Key1, Key2, Key3, COUNT(*) AS Cnt, MAX(LEN(Content)) AS MaxLen
FROM Ice.UXAppVersion WITH (NOLOCK)
WHERE TypeCode IN ('KNTCCustLayer','KNTCEventLayout','KNTCCustLayerBack')
GROUP BY TypeCode, Key1, Key2, Key3
ORDER BY TypeCode, Key1
"@
$cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
$rdr = $cmd.ExecuteReader()
while ($rdr.Read()) {
  Write-Output ("{0} | {1} | {2} | {3} | rows={4} maxlen={5}" -f $rdr['TypeCode'], $rdr['Key1'], $rdr['Key2'], $rdr['Key3'], $rdr['Cnt'], $rdr['MaxLen'])
}
$rdr.Close()
$conn.Close()
