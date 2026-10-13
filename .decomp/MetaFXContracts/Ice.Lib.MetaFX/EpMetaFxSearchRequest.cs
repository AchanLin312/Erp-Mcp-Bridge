namespace Ice.Lib.MetaFX;

/// <summary>
///     MetaFx search dialog request
/// </summary>
public class EpMetaFxSearchRequest
{
	/// <summary>
	///     like property. Example : like Part.PartNum
	/// </summary>
	public string like { get; set; }

	/// <summary>
	///     optional request properties
	/// </summary>
	public EpMetaFxSearchRequestProperty properties { get; set; }
}
