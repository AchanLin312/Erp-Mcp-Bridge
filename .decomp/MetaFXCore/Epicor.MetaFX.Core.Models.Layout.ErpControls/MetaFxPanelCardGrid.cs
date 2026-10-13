using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("metafx-panel-card-grid")]
public class MetaFxPanelCardGrid : MetaFxPanelCard
{
	public bool AutoFillContainer { get; set; }

	public GridModel GridModel { get; set; }

	public List<ProviderModel> ViewOptions { get; set; }

	public bool FullScreenButtonVisible { get; set; }

	public MetaFxPanelCardGrid()
	{
		base.HeaderComponents = new List<Component>();
		base.ExpandedHeaderComponents = new List<Component>();
	}
}
