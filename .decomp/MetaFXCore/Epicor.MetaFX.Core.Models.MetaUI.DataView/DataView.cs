using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.DataView;

public class DataView
{
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "table")]
	public string Table { get; set; }

	[JsonProperty(PropertyName = "staticFilter", NullValueHandling = NullValueHandling.Ignore)]
	public string StaticFilter { get; set; }

	[JsonProperty(PropertyName = "parent", NullValueHandling = NullValueHandling.Ignore)]
	public Parent Parent { get; set; }

	[JsonProperty(PropertyName = "sort", NullValueHandling = NullValueHandling.Ignore)]
	public string Sort { get; set; }

	[JsonProperty(PropertyName = "schema")]
	public string Schema { get; set; } = "Erp";

	[JsonProperty(PropertyName = "datasetId")]
	public string DatasetId { get; set; } = "";

	[JsonProperty(PropertyName = "data", NullValueHandling = NullValueHandling.Ignore)]
	public string Data { get; set; }

	[JsonProperty(PropertyName = "additionalColumns")]
	public string[] AdditionalColumns { get; set; }
}
