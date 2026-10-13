namespace Ice.Lib.MetaFX;

/// <summary>
///     Metafx search dialog property
/// </summary>
public class EpMetaFxSearchRequestProperty
{
	/// <summary>
	///     Default search form. Example : Default
	/// </summary>
	public string SearchForm { get; set; }

	/// <summary>
	///     Contains layers. example : ["CSF-India", "CUS-NoPurchaseContacts"]
	/// </summary>
	public string[] layers { get; set; }

	/// <summary>
	///     Device that triggered the request. Example : Desktop | Mobile
	/// </summary>
	public string deviceType { get; set; }

	/// <summary>
	///     view mode. example appBuilder
	/// </summary>
	public string mode { get; set; }

	/// <summary>
	///  Debug flag. Turn this on to retain intermittent processing files in the cache.
	/// </summary>
	public bool debug { get; set; }
}
