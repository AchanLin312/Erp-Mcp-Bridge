using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptCriteriaFilter
{
	public string Company { get; set; }

	public string RptDefID { get; set; }

	public string RptCriteriaSetID { get; set; }

	public int FilterID { get; set; }

	public string FilterName { get; set; }

	public string AdapterName { get; set; }

	public string LookupField { get; set; }

	public string FilterLabel { get; set; }

	public string TabLabel { get; set; }

	public string DataType { get; set; }

	public string EpiGuid { get; set; }

	public int DisplayOrder { get; set; }

	public bool SystemFlag { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
