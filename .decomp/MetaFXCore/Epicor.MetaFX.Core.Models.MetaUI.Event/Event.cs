using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.Event;

public class Event
{
	[JsonProperty(PropertyName = "id")]
	public string Name { get; set; }

	[JsonProperty(PropertyName = "trigger", NullValueHandling = NullValueHandling.Ignore)]
	public Trigger Trigger { get; set; }

	[JsonProperty(PropertyName = "action", NullValueHandling = NullValueHandling.Ignore)]
	public object Action { get; set; }
}
