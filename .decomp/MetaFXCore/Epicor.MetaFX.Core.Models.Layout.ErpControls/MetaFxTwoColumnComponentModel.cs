using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class MetaFxTwoColumnComponentModel : ComponentModel
{
	[JsonProperty(Order = int.MinValue)]
	public int? Columns { get; set; }
}
