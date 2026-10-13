namespace Epicor.MetaFX.Core.Models.Layers;

public class SaveLayerRequest
{
	public EpMetaFxLayer Layer { get; set; }

	public string CountryGroupCode { get; set; }

	public string CountryCode { get; set; }

	public bool PublishParentLayers { get; set; }
}
