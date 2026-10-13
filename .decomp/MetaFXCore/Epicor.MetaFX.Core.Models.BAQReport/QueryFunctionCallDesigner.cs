namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryFunctionCallDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string FunctionID { get; set; }

	public string ParameterName { get; set; }

	public int Seq { get; set; }

	public string Value { get; set; }

	public string DataType { get; set; }

	public bool SystemFlag { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
