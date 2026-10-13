using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class DynamicCriteriaParam
{
	public string AgentCompareString { get; set; }

	public string NumericFormat { get; set; }

	public string DateFormat { get; set; }

	public int ArchiveCode { get; set; }

	public string TaskNote { get; set; }

	public string WorkstationID { get; set; }

	public int ReportStyleNum { get; set; }

	public string RptVersion { get; set; }

	public string RptPrinterSettings { get; set; }

	public string RptPageSettings { get; set; }

	public bool RecurringTask { get; set; }

	public int AgentTaskNum { get; set; }

	public string AgentID { get; set; }

	public long AgentSchedNum { get; set; }

	public string PrinterName { get; set; }

	public string AutoAction { get; set; }

	public Guid SysRowID { get; set; }

	public string UserCriteria { get; set; }

	public string ReportTitle { get; set; }

	public string ReportID { get; set; }

	public string ProcessID { get; set; }

	public string ProcessCompany { get; set; }

	public string ProcessSystemCode { get; set; }

	public string EMailBody { get; set; }

	public string RowMod { get; set; }

	public bool DesignMode { get; set; }

	public bool SSRSEnableRouting { get; set; }

	public bool PrintReportParameters { get; set; }

	public string UIXml { get; set; }

	public string SSRSRenderFormat { get; set; }

	public string ReportCultureCode { get; set; }

	public string ReportCurrencyCode { get; set; }

	public string AttachmentType { get; set; }

	public int ProcessTaskNum { get; set; }

	public string EMailBCC { get; set; }

	public byte[] SpecifiedProperties { get; set; }

	public string EMailTo { get; set; }

	public string FaxNumber { get; set; }

	public string FaxTo { get; set; }

	public string FaxSubject { get; set; }

	public int GlbDecimalsPrice { get; set; }

	public int GlbDecimalsCost { get; set; }

	public int GlbDecimalsGeneral { get; set; }

	public int DecimalsPrice { get; set; }

	public int DecimalsCost { get; set; }

	public int DecimalsGeneral { get; set; }

	public string EMailCC { get; set; }
}
