namespace Epicor.MetaFX.Core.Models.BAQReport;

public class BAQReport
{
	public string Company { get; set; }

	public string BAQRptID { get; set; }

	public string Description { get; set; }

	public string ReportTitle { get; set; }

	public string FormTitle { get; set; }

	public string ExportID { get; set; }

	public bool SystemFlag { get; set; }

	public bool Completed { get; set; }

	public bool GlobalReport { get; set; }

	public bool IsCrystalReport { get; set; }

	public string ReportID { get; set; }

	public string CrystalReportName { get; set; }

	public string CGCCode { get; set; }

	public string SSRSReportName { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string TempRowID { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
