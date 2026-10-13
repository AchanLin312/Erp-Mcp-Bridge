namespace Ice.Lib.MetaFX;

/// <summary>
/// Model to run diff algorithm 
/// </summary>
public class EpMetaFxDiffRequest
{
	/// <summary>
	///     view id
	/// </summary>
	public string id { get; set; }

	/// <summary>
	///     optional request properties for left comparison tree
	/// </summary>
	public EpMetaFxRequestProperty leftRequestProperties { get; set; }

	/// <summary>
	///     optional request properties for right comparison tree
	/// </summary>
	public EpMetaFxRequestProperty rightRequestProperties { get; set; }
}
