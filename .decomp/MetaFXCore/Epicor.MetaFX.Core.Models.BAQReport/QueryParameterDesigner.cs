namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QueryParameterDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string ParameterID { get; set; }

	public string ParameterType { get; set; }

	public string ParameterLabel { get; set; }

	public bool SkipIfEmpty { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
