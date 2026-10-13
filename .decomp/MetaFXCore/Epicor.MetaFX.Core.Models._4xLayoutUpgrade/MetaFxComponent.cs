using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models._4xLayoutUpgrade;

public class MetaFxComponent<T>
{
	public List<object> Components;

	[JsonProperty(NullValueHandling = NullValueHandling.Include)]
	public string ParentId { get; set; }

	public string Id { get; set; }

	public string TargetTypeId { get; set; }

	public string SourceTypeId { get; set; }

	public T Model { get; set; }
}
