using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFxLayerCommon
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public virtual string ViewId { get; set; }

	[Required]
	public string TypeCode { get; set; }

	public string Company { get; set; }

	[StringLength(50, ErrorMessage = "Layer Name cannot be longer than 50 characters.")]
	public string LayerName { get; set; }

	[Required]
	public string DeviceType { get; set; }

	[StringLength(8, ErrorMessage = "CGC Code cannot be longer than 8 characters.")]
	public string CGCCode { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public StatusValue? UpgradeStatus { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string UpgradeError { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string DeleteError { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public bool SystemFlag { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string PublishError { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public StatusValue? PublishStatus { get; set; }

	public bool HasDraftContent { get; set; }

	public string LastUpdatedBy { get; set; }
}
