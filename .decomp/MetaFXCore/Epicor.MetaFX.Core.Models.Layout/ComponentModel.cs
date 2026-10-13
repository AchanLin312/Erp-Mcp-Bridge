using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout;

public class ComponentModel
{
	[JsonProperty(Order = int.MinValue)]
	public string Id { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string Class { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string FlexBasis { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string Guid { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string LabelText { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public bool Hidden { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public bool? IsKeyField { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string EpBinding { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public int? TabIndex { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public bool? Disabled { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public bool? Readonly { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string Alignment { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public int? MinHeight { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public bool? DisabledToolBar { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string EpiLabel { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public long? Height { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public long? Width { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public long? Top { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public long? Left { get; set; }
}
