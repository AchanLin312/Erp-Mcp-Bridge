using System;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFxLayerForApplication : EpMetaFxLayerCommon
{
	public string Id { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string SubType { get; set; }

	public DateTime? LastUpdated { get; set; }

	public bool IsPublished { get; set; }

	public bool IsSilentExport { get; set; }
}
