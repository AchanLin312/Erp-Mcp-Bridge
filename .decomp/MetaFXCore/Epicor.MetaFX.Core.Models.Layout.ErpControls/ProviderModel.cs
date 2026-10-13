namespace Epicor.MetaFX.Core.Models.Layout.ErpControls;

public class ProviderModel
{
	public string Description { get; set; }

	public bool IsDefault { get; set; }

	public bool ServerPaging { get; set; }

	public string EpBinding { get; set; }

	public string BaqId { get; set; }

	public BaqOptions BaqOptions { get; set; }

	public Columns[] Columns { get; set; }

	public bool AutoRefreshOnLoad { get; set; }
}
