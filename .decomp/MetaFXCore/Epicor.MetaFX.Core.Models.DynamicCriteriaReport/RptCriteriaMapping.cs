using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptCriteriaMapping
{
	public string Company { get; set; }

	public string RptDefID { get; set; }

	public string RptCriteriaSetID { get; set; }

	public string RptTableID { get; set; }

	public string ParameterID { get; set; }

	public int? PromptID { get; set; }

	public int? FilterID { get; set; }

	public bool SystemFlag { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
