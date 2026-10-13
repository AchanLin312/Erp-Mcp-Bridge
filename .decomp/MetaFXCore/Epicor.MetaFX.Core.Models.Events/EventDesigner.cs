using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Events;

public class EventDesigner
{
	public string ViewId { get; set; }

	public string Company { get; set; }

	public string TypeCode { get; set; }

	public string LayerName { get; set; }

	public string DeviceType { get; set; }

	public Dictionary<string, JToken> Content { get; set; }

	public string CGCCode { get; set; }

	public string[] ParentLayers { get; set; }

	public EventDesigner()
	{
		Content = new Dictionary<string, JToken>();
	}
}
