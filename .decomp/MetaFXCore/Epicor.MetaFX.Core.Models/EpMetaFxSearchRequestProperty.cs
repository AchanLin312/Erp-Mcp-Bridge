namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxSearchRequestProperty
{
	public string SearchForm { get; set; }

	public string[] layers { get; set; }

	public string deviceType { get; set; }

	public string mode { get; set; }

	public bool debug { get; set; }
}
