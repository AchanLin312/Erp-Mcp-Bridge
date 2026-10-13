using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

[ComponentModel("erp-numeric-box")]
public class ErpNumericBox : ComponentModel
{
	public bool Nullable { get; set; }

	public string Mask { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public double? Min { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public double? Max { get; set; }

	public long? Step { get; set; }
}
