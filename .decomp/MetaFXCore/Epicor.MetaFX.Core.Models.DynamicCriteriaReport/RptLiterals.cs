using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptLiterals
{
	public string RowMod { get; set; }

	public int BitFlag { get; set; }

	public Guid SysRowID { get; set; }

	public long SysRevID { get; set; }

	public bool SystemFlag { get; set; }

	public string LiteralValue { get; set; }

	public string LiteralName { get; set; }

	public string RptDefID { get; set; }
}
