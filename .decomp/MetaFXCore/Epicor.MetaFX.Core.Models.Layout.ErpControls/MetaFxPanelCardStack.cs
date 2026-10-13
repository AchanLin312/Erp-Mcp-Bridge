using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class MetaFxPanelCardStack : ComponentModel
{
	public string Title { get; set; }

	public List<PanelCardStackItem> Panels { get; set; }

	public string AutoSize { get; set; }

	public bool CurrentIndex { get; set; }

	public bool Expanded { get; set; }

	public bool DisableExpandCollapse { get; set; }
}
