using Epicor.MetaFX.Core.Contracts;

namespace Epicor.MetaFX.Core.Models;

public class Services
{
	public ILayerService LayerService { get; set; }

	public IEventDesignerService EventDesignerService { get; set; }
}
