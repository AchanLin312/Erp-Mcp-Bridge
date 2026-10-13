using System;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcInput
{
	public string Company { get; set; }

	public string PartNum { get; set; }

	public string RevisionNum { get; set; }

	public string InputName { get; set; }

	public int TabOrder { get; set; }

	public string DataType { get; set; }

	public string FormatString { get; set; }

	public string InitVal { get; set; }

	public string StatusText { get; set; }

	public bool Required { get; set; }

	public long PageSeq { get; set; }

	public string SideLabel { get; set; }

	public long XPos { get; set; }

	public long YPos { get; set; }

	public long PWidth { get; set; }

	public long PHeight { get; set; }

	public string ControlType { get; set; }

	public string PromptWhen { get; set; }

	public string ListItems { get; set; }

	public long StartDec { get; set; }

	public long EndDec { get; set; }

	public long IncrPrec { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public string ValList { get; set; }

	public bool Horizontal { get; set; }

	public bool IsGlobal { get; set; }

	public string OnLeave { get; set; }

	public string WebInputName { get; set; }

	public string SummaryLabel { get; set; }

	public bool DlRunProgram { get; set; }

	public string DlProgramName { get; set; }

	public string DlProgramInputs { get; set; }

	public string SourceDbRecid { get; set; }

	public bool HideInSummary { get; set; }

	public string OnLeave2 { get; set; }

	public bool Invisible { get; set; }

	public string Comments { get; set; }

	public bool GlobalInputVar { get; set; }

	public string GlbInputVarName { get; set; }

	public bool NoDispSummary { get; set; }

	public bool ExternalRef { get; set; }

	public string AttributeId { get; set; }

	public bool UseMinValue { get; set; }

	public bool UseMaxValue { get; set; }

	public bool UseIncrValue { get; set; }

	public bool UseInitVal { get; set; }

	public bool UseToolTip { get; set; }

	public bool UseScreenLabel { get; set; }

	public bool UseListValues { get; set; }

	public long SmartPartSeq { get; set; }

	public long SmartStringStart { get; set; }

	public long SmartStringEnd { get; set; }

	public bool ShowDistinct { get; set; }

	public string DesignControlType { get; set; }

	public string ConfigId { get; set; }

	public bool ReadOnly { get; set; }

	public long SysRevId { get; set; }

	public string SysRowId { get; set; }

	public bool AutoSizeList { get; set; }

	public int ListWidth { get; set; }

	public string ImageSource { get; set; }

	public string ImageLayerId { get; set; }

	public bool AllowSmartString { get; set; }

	public string AttributeDescription { get; set; }

	public bool CanUpdate { get; set; }

	public object Content { get; set; }

	public string DispCharVal { get; set; }

	public long DspPageSeq { get; set; }

	public string FullInputName { get; set; }

	public bool ImageMapping { get; set; }

	public object InitDate { get; set; }

	public long InitDecimal { get; set; }

	public bool InitLogical { get; set; }

	public bool InputRules { get; set; }

	public string InputType { get; set; }

	public bool IsGlobalInputVar { get; set; }

	public string OnLaunch { get; set; }

	public long PageDisplaySeq { get; set; }

	public long PcDynLstCount { get; set; }

	public string ReadOnlyExpression { get; set; }

	public bool TestPlan { get; set; }

	public string ImageUrl { get; set; }

	public long BitFlag { get; set; }

	public bool PartNumTrackDimension { get; set; }

	public string PartNumPricePerCode { get; set; }

	public bool PartNumTrackSerialNum { get; set; }

	public string PartNumPartDescription { get; set; }

	public long PartNumSellingFactor { get; set; }

	public bool PartNumTrackLots { get; set; }

	public string PartNumIum { get; set; }

	public string PartNumSalesUm { get; set; }

	public string RowMod { get; set; }
}
