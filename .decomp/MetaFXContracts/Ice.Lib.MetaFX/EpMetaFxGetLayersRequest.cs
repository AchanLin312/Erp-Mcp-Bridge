namespace Ice.Lib.MetaFX;

/// <summary>
///     EpMetaFxGetLayersRequest.
/// </summary>
public class EpMetaFxGetLayersRequest
{
	/// <summary>
	///     ViewId Property Ex: Erp.UIProc.MRP
	/// </summary>
	public string ViewId { get; set; }

	/// <summary>
	///     TypeCode Property Ex: Customization_MetaUI , Personalizaion_MetaUI
	/// </summary>
	public string TypeCode { get; set; }

	/// <summary>
	///     DeviceType Property
	/// </summary>
	public string DeviceType { get; set; }

	/// <summary>
	///     Include UnPublished layers
	/// </summary>
	public bool IncludeUnpublishedLayers { get; set; }
}
