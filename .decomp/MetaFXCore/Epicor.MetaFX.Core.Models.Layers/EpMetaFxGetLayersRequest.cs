namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFxGetLayersRequest
{
	public string ViewId { get; set; }

	public string ViewSearchText { get; set; }

	public string TypeCode { get; set; }

	public string DeviceType { get; set; }

	public string UserName { get; set; }

	public bool IncludeUnpublishedLayers { get; set; }

	public bool IncludePersLayers { get; set; }
}
