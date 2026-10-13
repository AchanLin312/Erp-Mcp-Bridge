$cs = 'Data Source=192.6.4.16;Initial Catalog=GPEDev;User ID=epicorread;Password=epicorread;TrustServerCertificate=true;Connect Timeout=30'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Key1, Key2, TypeCode, Key3, Version, LEN(Content) AS Len, CreatedBy, CreatedOn FROM Ice.UXAppVersion WITH (NOLOCK) WHERE Key1 IN ('JVerify','PipeTest','PipeTest2') ORDER BY CreatedOn DESC"
$r = $cmd.ExecuteReader()
$n = 0
while ($r.Read()) {
  $n++
  Write-Output ("{0} | {1} | {2} | {3} | v{4} | len={5} | {6} | {7}" -f $r['Key1'], $r['Key2'], $r['TypeCode'], $r['Key3'], $r['Version'], $r['Len'], $r['CreatedBy'], $r['CreatedOn'])
}
$r.Close()
Write-Output ("rows: {0}" -f $n)
$conn.Close()
