namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcInputsRule
{
	public string Company { get; set; }

	public string ConfigID { get; set; }

	public string TargetInputName { get; set; }

	public int RuleSeq { get; set; }

	public string RuleDesc { get; set; }

	public string TargetInputProperty { get; set; }

	public string TargetInputValue { get; set; }

	public int ProcessSeq { get; set; }

	public int SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string DefinitionDesc { get; set; }

	public int BitFlag { get; set; }

	public string RowMod { get; set; }
}
