using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Events;

public class EpMetaFxEventDesigner
{
	public string ViewId { get; set; }

	public string Company { get; set; }

	public string LayerName { get; set; }

	public string EventId { get; set; }

	public string DeviceType { get; set; }

	public JToken Content { get; set; }

	public string CGCCode { get; set; }
}
