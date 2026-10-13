using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.Strings;

public class ApplicationString
{
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "text")]
	public string Text { get; set; }

	[JsonProperty(PropertyName = "layer")]
	public bool Layer { get; set; }

	[JsonProperty(PropertyName = "missing")]
	public bool Missing { get; set; }

	internal ApplicationString(string id, string text, bool layer = false, bool missing = false)
	{
		Id = id;
		Text = text;
		Layer = layer;
		Missing = missing;
	}
}
