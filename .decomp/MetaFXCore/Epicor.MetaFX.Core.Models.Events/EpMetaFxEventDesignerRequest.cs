using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Epicor.MetaFX.Core.Models.Events;

public class EpMetaFxEventDesignerRequest
{
	[Required]
	public string ViewId { get; set; }

	public string Company { get; set; }

	public List<string> Layers { get; set; }

	[Required]
	public string EventId { get; set; }

	[Required]
	public string DeviceType { get; set; }
}
