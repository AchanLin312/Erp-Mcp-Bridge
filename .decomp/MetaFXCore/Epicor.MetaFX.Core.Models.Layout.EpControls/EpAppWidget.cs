namespace Epicor.MetaFX.Core.Models.Layout.EpControls;

public class EpAppWidget
{
	public string Id { get; set; }

	public string Description { get; set; }

	public string IconClass { get; set; }

	public string ImagePath { get; set; }

	public string Url { get; set; }

	public string SetupUrl { get; set; }

	public EpAppCapability[] Capabilities { get; set; }

	public EpAppWidgetModel Model { get; set; }

	public string OverrideEpEmbeddedApp { get; set; }
}
