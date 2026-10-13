using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models._4xLayoutUpgrade;

public class MetaFxPanelCardGrid
{
	public string Guid { get; set; }

	public Grid GridModel { get; set; }

	public List<object> HeaderComponents { get; set; }

	public List<object> ExpandedHeaderComponents { get; set; }

	public string Id { get; set; }

	public string AutoFillContainer { get; set; }

	public List<object> ViewOptions { get; set; }

	public int TabIndex { get; set; }

	public string Title { get; set; }

	public bool DisablePanelCardStyle { get; set; }

	public MetaFxPanelCardGrid()
	{
		HeaderComponents = new List<object>();
		ExpandedHeaderComponents = new List<object>();
	}
}
