using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models._4xLayoutUpgrade;

public class ColumnsItem
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string Id { get; set; }

	public string Field { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string Title { get; set; }
}
