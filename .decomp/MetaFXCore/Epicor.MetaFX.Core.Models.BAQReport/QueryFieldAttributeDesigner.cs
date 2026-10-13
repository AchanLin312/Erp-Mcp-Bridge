namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryFieldAttributeDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string TableID { get; set; }

	public string FieldName { get; set; }

	public string AttributeName { get; set; }

	public string Value { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public bool IsOverriden { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
