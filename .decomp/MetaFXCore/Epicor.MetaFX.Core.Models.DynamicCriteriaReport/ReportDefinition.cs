using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class ReportDefinition
{
	public List<DynamicCriteriaParam> DynamicCriteriaParam { get; set; }

	public List<ReportStyle> ReportStyle { get; set; }
}
