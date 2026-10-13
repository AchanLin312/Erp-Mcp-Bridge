namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryUpdateFieldDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string MapTableName { get; set; }

	public string MapFieldName { get; set; }

	public bool Direction { get; set; }

	public string Expression { get; set; }

	public bool IsKeyField { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string NetDataType { get; set; }

	public string Description { get; set; }

	public string QualifiedName { get; set; }

	public bool Required { get; set; }

	public string DataType { get; set; }

	public bool NullableType { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
