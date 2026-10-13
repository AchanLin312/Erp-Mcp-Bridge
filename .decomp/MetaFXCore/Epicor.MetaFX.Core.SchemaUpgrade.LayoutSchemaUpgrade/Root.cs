using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

public class Root
{
	[JsonProperty(PropertyName = "$schema")]
	public string schema { get; set; }

	public string contentVersion { get; set; }

	public JArray imports { get; set; }

	public dynamic layout { get; set; }

	public dynamic patch { get; set; }
}
