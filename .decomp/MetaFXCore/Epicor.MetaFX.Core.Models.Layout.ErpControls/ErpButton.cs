namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("erp-button")]
public class ErpButton : ComponentModel
{
	public const string SIZE_MEDIUM = "ep-btn-size-medium";

	public int? Style { get; set; }

	public string Size { get; set; }
}
