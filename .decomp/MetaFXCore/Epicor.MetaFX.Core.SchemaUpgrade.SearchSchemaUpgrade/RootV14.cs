using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

public class RootV14
{
	[JsonProperty(PropertyName = "$schema")]
	public string schema { get; set; }

	public string contentVersion { get; set; }

	public JArray imports { get; set; }

	public SearchV14 search { get; set; }

	public JArray patch { get; set; }
}
