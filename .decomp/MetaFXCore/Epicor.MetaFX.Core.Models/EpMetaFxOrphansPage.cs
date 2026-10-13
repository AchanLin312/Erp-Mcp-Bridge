using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Epicor.MetaFX.Core.Models.Layout;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxOrphansPage
{
	[Required]
	[JsonProperty(Order = int.MinValue)]
	public string name { get; set; }

	[JsonProperty(Order = int.MinValue)]
	public string guid { get; set; }

	[Required]
	public string caption { get; set; }

	public bool isOrphanPage { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<Component> components { get; set; }
}
