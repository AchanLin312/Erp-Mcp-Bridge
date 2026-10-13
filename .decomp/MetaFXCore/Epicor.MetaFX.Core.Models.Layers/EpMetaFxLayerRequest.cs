using System.ComponentModel.DataAnnotations;

namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFxLayerRequest
{
	[Required]
	public string ViewId { get; set; }

	public string Company { get; set; }

	[Required]
	public string TypeCode { get; set; }

	public string LayerName { get; set; }

	[Required]
	public string DeviceType { get; set; }

	public string[] ParentLayers { get; set; }

	[Required]
	public string UserName { get; set; }

	public string PageName { get; set; }

	public string CGCCode { get; set; }

	public bool IncludeDraftContent { get; set; }

	public int UxAppVersion { get; set; }
}
