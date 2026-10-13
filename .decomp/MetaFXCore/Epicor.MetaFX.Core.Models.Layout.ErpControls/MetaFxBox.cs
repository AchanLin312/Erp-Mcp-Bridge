using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("metafx-box")]
public class MetaFxBox : ComponentModel
{
	public string FlexDirection { get; set; }

	public string ColumnsWide { get; set; }

	public List<Component> components { get; set; }
}
