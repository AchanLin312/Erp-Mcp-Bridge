using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class ReportStyle
{
	public bool StructuredOutputEnabled { get; set; }

	public string RptStructuredOutputDefID { get; set; }

	public string RptCriteriaSetID { get; set; }

	public Guid SysRowID { get; set; }

	public long SysRevID { get; set; }

	public string CGCCode { get; set; }

	public bool SystemFlag { get; set; }

	public string OutputEDI { get; set; }

	public string OutputLocation { get; set; }

	public int ServerNum { get; set; }

	public string CompanyList { get; set; }

	public string RptDefID { get; set; }

	public string PrintProgramOptions { get; set; }

	public string PrintProgram { get; set; }

	public string RptTypeID { get; set; }

	public string StyleDescription { get; set; }

	public int StyleNum { get; set; }

	public string ReportID { get; set; }

	public bool RequireSubmissionID { get; set; }

	public bool AllowResetAfterSubmit { get; set; }

	public string CertificateID { get; set; }

	public string LangNameID { get; set; }

	public string RowMod { get; set; }

	public string RptTypeRptTypeDescription { get; set; }

	public string RptDefRptDescription { get; set; }

	public string ReportRptDescription { get; set; }

	public int BitFlag { get; set; }

	public string Company { get; set; }

	public string LangNameIDDescription { get; set; }

	public string StatusMessage { get; set; }

	public int Status { get; set; }

	public DateTime? CertExpiration { get; set; }

	public bool CertificateIsSystem { get; set; }

	public bool CertificateIsAllComp { get; set; }

	public bool RoutingRuleEnabled { get; set; }

	public bool HasBAQOrEI { get; set; }

	public string FormatCulture { get; set; }

	public bool RptDefSystemFlag { get; set; }
}
