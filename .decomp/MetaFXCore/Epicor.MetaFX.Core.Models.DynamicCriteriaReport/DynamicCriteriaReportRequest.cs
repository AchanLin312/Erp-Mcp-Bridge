using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class DynamicCriteriaReportRequest
{
	public string SystemCode { get; set; }

	public ReportCriteriaDefinition ReportCriteriaDefinition { get; set; }

	public List<ReportCriteriaDefinition> ReportCriteriaDefinitions { get; set; }

	public ReportDefinition ReportDefinition { get; set; }

	public RptCriteriaFilterAdapterXRef RptCriteriaFilterAdapterXRef { get; set; }

	public DynamicCriteriaReportRequest(string systemCode)
	{
		SystemCode = systemCode;
	}
}
