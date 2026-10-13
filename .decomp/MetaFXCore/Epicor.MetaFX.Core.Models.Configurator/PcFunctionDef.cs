namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcFunctionDef
{
	public string Company { get; set; }

	public string FunctionName { get; set; }

	public string Description { get; set; }

	public string ConfigID { get; set; }

	public string FunctionType { get; set; }

	public string Expression { get; set; }

	public string ReturnType { get; set; }

	public string OldFunctionName { get; set; }

	public bool IsSync { get; set; }

	public bool GlobalFunc { get; set; }

	public string SysRevID { get; set; }

	public string SysRowID { get; set; }

	public string BagID { get; set; }

	public bool NoInputs { get; set; }

	public string ScriptExpression { get; set; }

	public string DispFunctionName { get; set; }

	public string DispFunctionType { get; set; }

	public string DispReturnType { get; set; }

	public bool BtnEditScript { get; set; }

	public int BitFlag { get; set; }

	public string PcStatusConfigType { get; set; }

	public bool PcStatusApproved { get; set; }

	public string RowMod { get; set; }

	public bool isDynamicListUD { get; set; }
}
