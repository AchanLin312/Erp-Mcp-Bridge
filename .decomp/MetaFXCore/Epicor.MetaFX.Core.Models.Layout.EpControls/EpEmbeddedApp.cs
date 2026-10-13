namespace Epicor.MetaFX.Core.Models.Layout.EpControls;

[ComponentModel("ep-embedded-app")]
public class EpEmbeddedApp : ComponentModel
{
	public string WidgetType { get; set; }

	public EpAppWidget EmbeddedAppWidget { get; set; }

	public string IconClass { get; set; }

	public int WidgetHeight { get; set; }

	public object[] Capabilities { get; set; }
}
