namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryTableDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string TableID { get; set; }

	public int Seq { get; set; }

	public string DBSchemaName { get; set; }

	public string DBTableName { get; set; }

	public string TableType { get; set; }

	public bool IsSummaryTable { get; set; }

	public bool IsVisibleInDesigner { get; set; }

	public string Modifiers { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string PivotFunction { get; set; }

	public string PivotDataType { get; set; }

	public string PivotFieldFormat { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
