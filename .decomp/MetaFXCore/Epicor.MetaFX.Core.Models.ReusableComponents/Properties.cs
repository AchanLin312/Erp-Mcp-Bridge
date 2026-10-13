using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.ReusableComponents;

public class Properties
{
	[JsonProperty("key")]
	public string Key { get; set; }

	[JsonProperty("value")]
	public dynamic Value { get; set; }
}
