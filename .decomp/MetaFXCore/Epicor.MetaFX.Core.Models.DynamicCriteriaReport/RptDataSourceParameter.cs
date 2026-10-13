using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptDataSourceParameter
{
	public string RptCriteriaSetID { get; set; }

	public string RptTableID { get; set; }

	public string ParameterID { get; set; }

	public int PromptID { get; set; }

	public int FilterID { get; set; }

	public string Type { get; set; }

	public string ParamDataType { get; set; }

	public bool IsMandatory { get; set; }

	public bool SkipIfEmpty { get; set; }

	public string ControlType { get; set; }

	public string ControlName { get; set; }

	public string ControlDataType { get; set; }

	public string QueryEI { get; set; }

	public long SysRevID { get; set; }

	public bool Selection { get; set; }

	public string SourceControlType { get; set; }

	public Guid SysRowID { get; set; }

	public string RowMod { get; set; }
}
