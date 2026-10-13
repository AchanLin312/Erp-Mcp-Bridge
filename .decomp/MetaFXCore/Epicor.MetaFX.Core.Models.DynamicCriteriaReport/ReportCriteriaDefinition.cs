using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class ReportCriteriaDefinition
{
	public string Name { get; set; }

	public List<RptDataDef> RptDataDef { get; set; }

	public List<RptTable> RptTable { get; set; }

	public List<RptCriteriaSet> RptCriteriaSet { get; set; }

	public List<RptCriteriaFilter> RptCriteriaFilter { get; set; }

	public List<RptCriteriaMapping> RptCriteriaMapping { get; set; }

	public List<RptCriteriaPrompt> RptCriteriaPrompt { get; set; }

	public List<RptCriteriaPromptValue> RptCriteriaPromptValue { get; set; }

	public List<RptLiterals> RptLiterals { get; set; }

	public List<RptDataSourceField> RptDataSourceField { get; set; }

	public List<RptDataSourceParameter> RptDataSourceParameter { get; set; }

	public List<RptCriteriaFilterAdapterXRef> RptCriteriaFilterAdapterXRef { get; set; }
}
