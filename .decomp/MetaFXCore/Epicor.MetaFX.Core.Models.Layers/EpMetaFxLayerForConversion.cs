using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFxLayerForConversion : EpMetaFxLayer
{
	public bool? HasDraftConverted { get; set; }

	public bool? HasPublishedConverted { get; set; }

	public List<EpMetaFxLayerForConversion> AllParentLayers { get; set; }
}
