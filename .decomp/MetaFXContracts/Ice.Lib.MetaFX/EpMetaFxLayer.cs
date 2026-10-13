using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Ice.Lib.MetaFX;

/// <summary>
///     EpMetaFxLayer
/// </summary>
public class EpMetaFxLayer
{
	/// <summary>
	///     Company Property
	/// </summary>
	public string Company { get; set; }

	/// <summary>
	///     TypeCode Property Ex: Customization_MetaUI , Personalizaion_MetaUI
	/// </summary>
	public string TypeCode { get; set; }

	/// <summary>
	///     ViewId Property Ex: Erp.UIProc.MRP
	/// </summary>
	public string ViewId { get; set; }

	/// <summary>
	///     LayerName Property
	/// </summary>
	public string LayerName { get; set; }

	/// <summary>
	///     LayerDescription Property
	/// </summary>
	public string LayerDescription { get; set; }

	/// <summary>
	///     List of parent layers
	/// </summary>
	public string[] ParentLayers { get; set; }

	/// <summary>
	///     CommentText Property
	/// </summary>
	public string CommentText { get; set; }

	/// <summary>
	///     WIP Property
	/// </summary>
	public bool WIP { get; set; }

	/// <summary>
	///     DeviceType Property
	/// </summary>
	public string DeviceType { get; set; }

	/// <summary>
	///     IsNew Property
	/// </summary>
	public bool IsNew { get; set; }

	/// <summary>
	///     Content Property
	/// </summary>
	public Dictionary<string, JToken> Content { get; set; }
}
