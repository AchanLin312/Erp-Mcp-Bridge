namespace Epicor.MetaFX.Core.Models.Layers;

public class CustomizationLayerRequest
{
	public string ViewId { get; set; }

	public string Company { get; set; }

	public string TypeCode { get; set; }

	public string[] Layers { get; set; }

	public string DeviceType { get; set; }

	public string UserName { get; set; }

	public string LayerType { get; set; }

	public string CountryGroupCode { get; set; }

	public bool IsDesignerMode { get; set; }

	public string CountryCode { get; set; }

	public bool IncludeUnPublishedLayers { get; set; }

	public int UxAppVersion { get; set; }
}
