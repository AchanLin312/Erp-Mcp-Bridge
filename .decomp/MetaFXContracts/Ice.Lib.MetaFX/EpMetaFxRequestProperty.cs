using System.Collections.Generic;

namespace Ice.Lib.MetaFX;

/// <summary>
/// Model for left and right application to compare.
/// </summary>
public class EpMetaFxRequestProperty
{
	/// <summary>
	///     Contains layers. example : ["CSF-India", "CUS-NoPurchaseContacts"]
	/// </summary>
	public string[] layers { get; set; }

	/// <summary>
	///     layerType - Can be KNTCPersLayer or KNTCCustLayer
	/// </summary>
	public string layerType { get; set; }

	/// <summary>
	///     Device who triggered the request. Example : Desktop | Mobile
	/// </summary>
	public string deviceType { get; set; }

	/// <summary>
	///     view mode. example appBuilder
	/// </summary>
	public string mode { get; set; }

	/// <summary>
	///     userName
	/// </summary>
	public string userName { get; set; }

	/// <summary>
	///     pageName
	/// </summary>
	public string pageName { get; set; }

	/// <summary>
	///     company
	/// </summary>
	public string company { get; set; }

	/// <summary>
	///     country group code
	/// </summary>
	public string countryGroupCode { get; set; }

	/// <summary>
	///     debug
	/// </summary>
	public bool debug { get; set; }

	/// <summary>
	///     Country Code
	/// </summary>
	public string countryCode { get; set; }

	/// <summary>
	/// If true, wasm file will be returned.
	/// </summary>
	public bool includeWasm { get; set; }

	/// <summary>
	///     Type. Example: like 'Component'
	/// </summary>
	public string applicationType { get; set; }

	/// <summary>
	/// Additional platform specific context related to the view being requested.
	/// </summary>
	public Dictionary<string, object> additionalContext { get; set; }

	/// <summary>
	///     checkDuplicateIds
	/// </summary>
	public bool checkDuplicateIds { get; set; }

	/// <summary>
	///     layerVersion
	/// </summary>
	public int layerVersion { get; set; }

	/// <summary>
	///     baseAppVersion
	/// </summary>
	public int baseAppVersion { get; set; }

	/// <summary>
	///  basis will decide the purpose of the request.
	///  ex: null tells request is for getting app json, 
	///  ApplicationDiff means request is for app difference 
	/// </summary>
	public string basis { get; set; }

	/// <summary>
	/// A flag to ignore personalization layer
	/// </summary>
	public bool ignorePersonalization { get; set; }
}
