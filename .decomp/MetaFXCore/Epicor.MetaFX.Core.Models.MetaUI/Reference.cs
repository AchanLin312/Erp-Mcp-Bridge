using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI;

public class Reference
{
	[JsonProperty(PropertyName = "$ref", Order = 1)]
	public string Refs { get; set; }
}
