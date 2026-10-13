using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;

namespace Epicor.MetaFX.Core.Models.Layers;

public class LayerRequest
{
	public EpMetaFxRequest Request { get; set; }

	public string RootPath { get; set; }

	public bool ApplyPersonalization { get; set; }

	public bool ApplyPatches { get; set; }

	public EpMetaFxLayer PersonalizedLayer { get; set; }

	public List<EpMetaFxLayer> CustomizedLayers { get; set; }

	public string CacheKey { get; set; }

	public ILayerService LayerService { get; set; }
}
