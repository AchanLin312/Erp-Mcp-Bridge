using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class ActionData
{
	public string Id { get; set; }

	public string Description { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Include)]
	public string Icon { get; set; }

	public bool ShowInToolBar { get; set; }

	public UserData UserData { get; set; }
}
