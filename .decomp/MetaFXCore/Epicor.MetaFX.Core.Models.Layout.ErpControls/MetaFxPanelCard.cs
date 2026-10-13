using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("metafx-panel-card")]
public class MetaFxPanelCard : ComponentModel
{
	public bool Expanded { get; set; }

	public bool HighLightPanelBar { get; set; }

	public string Title { get; set; }

	public bool ExpandMore { get; set; }

	public bool EnableActionButton { get; set; }

	public List<Component> HeaderComponents { get; set; }

	public List<Component> ExpandedHeaderComponents { get; set; }

	public List<Component> ExtensionComponents { get; set; }

	public Footer Footer { get; set; }

	public List<ActionData> ActionData { get; set; }

	public List<Component> BodyComponents { get; set; }

	public bool Stacked { get; set; }

	public MetaFxPanelCard()
	{
		HeaderComponents = new List<Component>();
		ExpandedHeaderComponents = new List<Component>();
		BodyComponents = new List<Component>();
		ExtensionComponents = new List<Component>();
		Footer = new Footer();
		ActionData = new List<ActionData>();
	}
}
