using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Layers;

public class JsonDiffInfo
{
	public string Id { get; set; }

	public string Path { get; set; }

	public JToken Value { get; set; }

	public OperationType Operation { get; set; }

	public int Index { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string FullPath { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public JToken PreValue { get; set; }
}
