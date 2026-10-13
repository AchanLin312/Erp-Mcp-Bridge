using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.Tools;

public class ToolsSchema : BaseSchema
{
	[JsonProperty(PropertyName = "tools", Order = 10)]
	public object[] Tools { get; set; } = new object[0];
}
