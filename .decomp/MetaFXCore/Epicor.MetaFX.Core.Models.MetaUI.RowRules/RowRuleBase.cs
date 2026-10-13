using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.RowRules;

public class RowRuleBase
{
	[JsonProperty(PropertyName = "id", Order = 1, NullValueHandling = NullValueHandling.Ignore)]
	public string Name { get; set; }

	[JsonProperty(PropertyName = "dataView", Order = 100, NullValueHandling = NullValueHandling.Ignore)]
	public string DataView { get; set; } = string.Empty;

	[JsonProperty(PropertyName = "condition", Order = 101, NullValueHandling = NullValueHandling.Ignore)]
	public string Condition { get; set; } = string.Empty;

	[JsonProperty(PropertyName = "actions", Order = 102, NullValueHandling = NullValueHandling.Ignore)]
	public object[] Actions { get; set; } = new object[0];
}
