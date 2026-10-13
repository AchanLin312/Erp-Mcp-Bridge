using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptDataDef
{
	public string Company { get; set; }

	public string RptDefID { get; set; }

	public string RptDescription { get; set; }

	public bool SystemRpt { get; set; }

	public string RptTypeID { get; set; }

	public string DuplicateOf { get; set; }

	public string CGCCode { get; set; }

	public string PrimaryTable { get; set; }

	public string PrimaryKey1 { get; set; }

	public string PrimaryKey2 { get; set; }

	public bool UseMultipleCriteria { get; set; }

	public bool SystemFlag { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
