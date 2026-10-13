using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptTable
{
	public string RptDefID { get; set; }

	public string RptTableID { get; set; }

	public string SystemCode { get; set; }

	public string ZDataTableID { get; set; }

	public decimal SeqControl { get; set; }

	public string ParentRptTableID { get; set; }

	public bool PrimaryTable { get; set; }

	public bool SystemFlag { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public bool IsSystemTable { get; set; }

	public string QueryID { get; set; }

	public string EFTHeadCompany { get; set; }

	public int? EFTHeadUID { get; set; }

	public string TableType { get; set; }

	public string DataSourceType { get; set; }

	public string DataSourceDescription { get; set; }

	public string DataSourceName { get; set; }

	public bool IsValidDataSource { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
