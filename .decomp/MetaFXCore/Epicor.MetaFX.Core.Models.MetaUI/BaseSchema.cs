using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI;

public class BaseSchema
{
	[JsonProperty(PropertyName = "$schema", Order = 1)]
	public string Schema { get; set; }

	[JsonProperty(PropertyName = "contentVersion", Order = 2)]
	public string ContentVersion { get; set; } = "1.0";

	[JsonProperty(PropertyName = "imports", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
	public object[] Imports { get; set; } = new object[0];

	[JsonProperty(PropertyName = "patch", Order = 101, NullValueHandling = NullValueHandling.Ignore)]
	public object Patch { get; set; }
}
