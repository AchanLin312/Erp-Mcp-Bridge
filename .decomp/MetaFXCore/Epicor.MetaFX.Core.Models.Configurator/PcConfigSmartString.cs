namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcConfigSmartString
{
	public string Company { get; set; }

	public string ConfigID { get; set; }

	public string StringStyle { get; set; }

	public string PrefacePart { get; set; }

	public string CrtCustPart { get; set; }

	public string Separator { get; set; }

	public object NumberFormat { get; set; }

	public string StartNumber { get; set; }

	public object TargetEntityForSmartString { get; set; }

	public string SysRowID { get; set; }

	public object RowMod { get; set; }
}
