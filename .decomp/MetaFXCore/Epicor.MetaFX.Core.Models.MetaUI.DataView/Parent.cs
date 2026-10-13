using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.DataView;

public class Parent
{
	[JsonProperty(PropertyName = "parentDataview")]
	public string ParentDataview { get; set; }

	[JsonProperty(PropertyName = "parentColumns")]
	public string[] ParentColumns { get; set; }

	[JsonProperty(PropertyName = "childColumns")]
	public string[] ChildColumns { get; set; }
}
