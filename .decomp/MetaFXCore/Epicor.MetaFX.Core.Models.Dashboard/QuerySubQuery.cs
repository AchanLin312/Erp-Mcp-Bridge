using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class QuerySubQuery
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

	public decimal TopRowExpr { get; set; }

	public bool TopInPercent { get; set; }

	public bool TopWithTies { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public string OrderByOffSet { get; set; }

	public string OrderByFetch { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
