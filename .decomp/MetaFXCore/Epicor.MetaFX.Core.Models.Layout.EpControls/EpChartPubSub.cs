using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Layout.EpControls;

public class EpChartPubSub
{
	public string Id { get; set; }

	public List<EpChartSubscribers> Subscribers { get; set; }
}
