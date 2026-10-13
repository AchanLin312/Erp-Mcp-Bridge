using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("erp-website")]
public class ErpWebsite : ComponentModel
{
	public string Url { get; set; }

	public string WidgetType { get; set; }

	public long WidgetHeight { get; set; }

	public long WidgetWidth { get; set; }

	public string IconClass { get; set; }

	public string Title { get; set; }

	public bool UserContext { get; set; }

	public bool Token { get; set; }

	public List<Capability> Capabilities { get; set; }

	public ErpWebsite()
	{
		Capabilities = new List<Capability>();
	}

	public bool ShouldSerializeWidgetWidth()
	{
		return WidgetWidth != 0;
	}

	public bool ShouldSerializeWidgetHeight()
	{
		return WidgetWidth != 0;
	}
}
