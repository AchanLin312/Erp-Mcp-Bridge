namespace Epicor.MetaFX.Core.Models.BAQReport;

public class QuerySubQueryDesigner
{
	public string Company { get; set; }

	public string QueryID { get; set; }

	public string SubQueryID { get; set; }

	public string Name { get; set; }

	public string Type { get; set; }

	public int Seq { get; set; }

	public bool IsUnion { get; set; }

	public string LeftP { get; set; }

	public string RightP { get; set; }

	public string SelectListClause { get; set; }

	public string TopRowExpr { get; set; }

	public bool TopInPercent { get; set; }

	public bool TopWithTies { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string OrderByOffSet { get; set; }

	public string OrderByFetch { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
