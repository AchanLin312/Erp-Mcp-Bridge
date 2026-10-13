using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptCriteriaSet
{
	public string RowMod { get; set; }

	public int BitFlag { get; set; }

	public Guid SysRowID { get; set; }

	public long SysRevID { get; set; }

	public bool IsDefault { get; set; }

	public bool SystemFlag { get; set; }

	public string Description { get; set; }

	public string RptCriteriaSetID { get; set; }

	public string RptDefID { get; set; }

	public string Company { get; set; }
}
