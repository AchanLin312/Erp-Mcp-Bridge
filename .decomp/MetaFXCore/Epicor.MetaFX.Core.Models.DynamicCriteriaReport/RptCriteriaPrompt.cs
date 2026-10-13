using System;

namespace Epicor.MetaFX.Core.Models.DynamicCriteriaReport;

public class RptCriteriaPrompt
{
	public string Company { get; set; }

	public bool SystemFlag { get; set; }

	public int DisplayOrder { get; set; }

	public bool IsVisible { get; set; }

	public string ConstantValue { get; set; }

	public bool IsConstant { get; set; }

	public string EpiGuid { get; set; }

	public string DefaultValue { get; set; }

	public string Label { get; set; }

	public string DataType { get; set; }

	public string PromptName { get; set; }

	public int PromptID { get; set; }

	public string RptCriteriaSetID { get; set; }

	public string RptDefID { get; set; }

	public long SysRevID { get; set; }

	public Guid SysRowID { get; set; }

	public string EditorType { get; set; }

	public string DataFrom { get; set; }

	public string QueryID { get; set; }

	public string DisplayColumn { get; set; }

	public string ValueColumn { get; set; }

	public string EFTHeadCompany { get; set; }

	public int? EFTHeadUID { get; set; }

	public bool IsEmptyString { get; set; }

	public string EIName { get; set; }

	public string EIDescription { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
