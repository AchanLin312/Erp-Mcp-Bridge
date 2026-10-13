using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

public class Root
{
	[JsonProperty(PropertyName = "$schema")]
	public string schema { get; set; }

	public string contentVersion { get; set; }

	public JArray imports { get; set; }

	public List<EventV11> events { get; set; }

	public JArray patch { get; set; }
}
