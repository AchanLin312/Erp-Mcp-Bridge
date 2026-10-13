namespace Epicor.MetaFX.Core.Models;

public class Layer
{
	public string company { get; set; }

	public string typeCode { get; set; }

	public string layerName { get; set; }

	public string layerDescription { get; set; }

	public string[] parentLayers { get; set; }

	public bool wip { get; set; }

	public bool IsNew { get; set; }

	public string CGCCode { get; set; }

	public bool PublishParentLayers { get; set; }
}
