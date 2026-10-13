using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layers;

public class PageInfo
{
	public string Name { get; set; }

	public string Version { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<JsonDiffInfo> Diffs { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public OperationType? Operation { get; set; }
}
