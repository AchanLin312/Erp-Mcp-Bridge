using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

public class TabDataComponent
{
	[JsonProperty(PropertyName = "components", Order = 6, NullValueHandling = NullValueHandling.Ignore)]
	public JArray components = new JArray();

	[JsonProperty(PropertyName = "id", Order = 1, NullValueHandling = NullValueHandling.Ignore)]
	public string id { get; set; }

	[JsonProperty(PropertyName = "title", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
	public string title { get; set; }

	[JsonProperty(PropertyName = "page", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
	public string page { get; set; }

	[JsonProperty(PropertyName = "selected", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
	public bool selected { get; set; }
}
