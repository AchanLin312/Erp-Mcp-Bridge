namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcPage
{
	public string Company { get; set; }

	public string PartNum { get; set; }

	public string RevisionNum { get; set; }

	public long PageSeq { get; set; }

	public string PageTitle { get; set; }

	public string PromptWhen { get; set; }

	public string OnLeave { get; set; }

	public bool SkipOnLeaveOpl { get; set; }

	public string OnLeave2 { get; set; }

	public string Comments { get; set; }

	public bool ProcessDynLstFirst { get; set; }

	public bool DynLstHigher { get; set; }

	public bool SkipPageProcessOl { get; set; }

	public bool SkipPageNoInputs { get; set; }

	public bool SkipOnLeaveOpe { get; set; }

	public long PWidth { get; set; }

	public long PHeight { get; set; }

	public long DisplaySeq { get; set; }

	public string ConfigId { get; set; }

	public bool ReadOnly { get; set; }

	public long SysRevId { get; set; }

	public string SysRowId { get; set; }

	public bool CanUpdate { get; set; }

	public bool FirstPage { get; set; }

	public bool TestPlan { get; set; }

	public string ReadOnlyExpression { get; set; }

	public long BitFlag { get; set; }

	public string RowMod { get; set; }
}
