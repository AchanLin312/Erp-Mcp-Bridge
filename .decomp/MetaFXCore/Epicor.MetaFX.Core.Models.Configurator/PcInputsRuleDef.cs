namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcInputsRuleDef
{
	public string Company { get; set; }

	public string ConfigID { get; set; }

	public string TargetInputName { get; set; }

	public int RuleSeq { get; set; }

	public int SeqNum { get; set; }

	public string SourceInputName { get; set; }

	public string SourceInputProperty { get; set; }

	public string SourceCharacterValue { get; set; }

	public int SourceIntegerValue { get; set; }

	public decimal SourceDecimalValue { get; set; }

	public object SourceDateValue { get; set; }

	public bool SourceLogicalValue { get; set; }

	public int ProcessSeq { get; set; }

	public string LeftP { get; set; }

	public string RightP { get; set; }

	public string AndOr { get; set; }

	public string CompOp { get; set; }

	public bool Neg { get; set; }

	public int SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string ListValues { get; set; }

	public string SourceValue { get; set; }

	public string InputType { get; set; }

	public int BitFlag { get; set; }

	public string SourcePcInputsDesignControlType { get; set; }

	public string SourcePcInputsFormatString { get; set; }

	public string SourcePcInputsListItems { get; set; }

	public string SourcePcInputsDataType { get; set; }

	public string RowMod { get; set; }
}
