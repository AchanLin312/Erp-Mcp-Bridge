namespace Ice.Lib.MetaFX;

/// <summary>
///     MetaFx Peek Request property
/// </summary>
public class EpMetaFxPeekRequestProperty
{
	/// <summary>
	///     Default peek form. Example : Default
	/// </summary>
	public string peekForm { get; set; }

	/// <summary>
	///     Device who triggered the request. Example : Desktop | Mobile
	/// </summary>
	public string deviceType { get; set; }

	/// <summary>
	///     view mode. example appBuilder
	/// </summary>
	public string mode { get; set; }
}
