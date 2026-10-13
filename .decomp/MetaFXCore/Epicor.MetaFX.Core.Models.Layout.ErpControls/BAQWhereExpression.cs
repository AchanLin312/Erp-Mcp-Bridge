using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class BAQWhereExpression
{
	public string Column { get; set; }

	public string Condition { get; set; }

	public string Value { get; set; }

	public bool HonorNull { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Include)]
	public object EmptyValue { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Include)]
	public bool CompareColumns { get; set; }
}
