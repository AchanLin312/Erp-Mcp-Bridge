using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Applications;

public class Application
{
	[Required]
	public string Id { get; set; }

	[Required]
	public string Type { get; set; }

	[Required]
	public string SubType { get; set; }

	public DateTime? LastUpdated { get; set; }

	public bool IsLayerDisabled { get; set; }

	public bool SystemFlag { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public bool? HasDraftContent { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string PublishError { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public StatusValue? PublishStatus { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<EpMetaFxLayerForApplication> Layers { get; set; }

	public string CreatedBy { get; set; }

	public bool? CanAccessBase { get; set; }

	public string SecurityCode { get; set; }

	public string LastUpdatedBy { get; set; }
}
