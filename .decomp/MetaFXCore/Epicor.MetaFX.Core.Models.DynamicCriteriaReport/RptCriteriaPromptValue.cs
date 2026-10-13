using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptCriteriaPromptValue
{
	public string Company { get; set; }

	public string RptDefID { get; set; }

	public string RptCriteriaSetID { get; set; }

	public int PromptID { get; set; }

	public int ItemID { get; set; }

	public string ItemValue { get; set; }

	public string ItemDescription { get; set; }

	public int DisplayOrder { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public bool SystemFlag { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
