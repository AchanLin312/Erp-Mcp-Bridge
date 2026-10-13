using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.Event;

public class Trigger
{
	[JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
	public string Type { get; set; }

	[JsonProperty(PropertyName = "target", NullValueHandling = NullValueHandling.Ignore)]
	public string Target { get; set; }

	[JsonProperty(PropertyName = "hook", NullValueHandling = NullValueHandling.Ignore)]
	public string Hook { get; set; }

	[JsonProperty(PropertyName = "columns", NullValueHandling = NullValueHandling.Ignore)]
	public List<string> Columns { get; set; }
}
