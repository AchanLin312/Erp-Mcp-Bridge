using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.ReusableComponents;

public class ComponentProperties
{
	[JsonProperty("controlId")]
	public string ControlId { get; set; }

	[JsonProperty("data")]
	public List<Properties> Data { get; set; }
}
