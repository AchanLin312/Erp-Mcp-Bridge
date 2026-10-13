namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryWhereItemDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string TableID { get; set; }

	public string CriteriaID { get; set; }

	public int Seq { get; set; }

	public string FieldName { get; set; }

	public string DataType { get; set; }

	public string CompOp { get; set; }

	public string AndOr { get; set; }

	public bool Neg { get; set; }

	public string LeftP { get; set; }

	public string RightP { get; set; }

	public bool IsConst { get; set; }

	public int CriteriaType { get; set; }

	public bool UseHaving { get; set; }

	public string ToTableID { get; set; }

	public string ToFieldName { get; set; }

	public string ToDataType { get; set; }

	public string RValue { get; set; }

	public bool ExtSecurity { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
