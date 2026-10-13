namespace Ice.Lib.MetaFX;

/// <summary>
///     MetaFx Peek Request
/// </summary>
public class EpMetaFxPeekRequest
{
	/// <summary>
	///     like property. Example : like Part.PartNum
	/// </summary>
	public string like { get; set; }

	/// <summary>
	///     request properties
	/// </summary>
	public EpMetaFxPeekRequestProperty properties { get; set; }
}
