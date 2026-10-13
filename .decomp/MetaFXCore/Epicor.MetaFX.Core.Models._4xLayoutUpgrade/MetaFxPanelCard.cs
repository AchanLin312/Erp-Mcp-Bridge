using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models._4xLayoutUpgrade;

public class MetaFxPanelCard
{
	public string Guid { get; set; }

	public List<object> HeaderComponents { get; set; }

	public List<object> ExpandedHeaderComponents { get; set; }

	public List<object> BodyComponents { get; set; }

	public List<object> ExtensionComponents { get; set; }

	public string Title { get; set; }

	public string Id { get; set; }

	public int TabIndex { get; set; }

	public MetaFxPanelCard()
	{
		HeaderComponents = new List<object>();
		ExpandedHeaderComponents = new List<object>();
		BodyComponents = new List<object>();
		ExtensionComponents = new List<object>();
	}
}
