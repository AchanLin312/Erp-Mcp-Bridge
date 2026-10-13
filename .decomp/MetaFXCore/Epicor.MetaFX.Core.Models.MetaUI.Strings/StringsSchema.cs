using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.Strings;

public class StringsSchema : BaseSchema
{
	[JsonProperty(PropertyName = "strings", Order = 10)]
	public object Strings { get; set; } = new object();
}
