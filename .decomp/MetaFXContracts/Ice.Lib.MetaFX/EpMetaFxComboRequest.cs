namespace Ice.Lib.MetaFX;

/// <summary>
///     Combo request
/// </summary>
public class EpMetaFxComboRequest
{
	/// <summary>
	///     folder property. Example : like Part.PartNum
	/// </summary>
	public string folder { get; set; }

	/// <summary>
	///     file property. Example : like Default
	/// </summary>
	public string subfolder { get; set; }
}
