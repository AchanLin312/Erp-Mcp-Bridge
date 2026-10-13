using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxRequestProperty
{
	public string[] layers { get; set; }

	public string layerType { get; set; }

	public string deviceType { get; set; }

	public string mode { get; set; }

	public string userName { get; set; }

	public string pageName { get; set; }

	public string company { get; set; }

	public string countryGroupCode { get; set; }

	public bool debug { get; set; }

	public string countryCode { get; set; }

	public bool includeWasm { get; set; }

	public string applicationType { get; set; }

	public Dictionary<string, object> additionalContext { get; set; }

	public bool checkDuplicateIds { get; set; }

	public int layerVersion { get; set; }

	public int baseAppVersion { get; set; }

	public string basis { get; set; }

	public bool ignorePersonalization { get; set; }

	public bool discardDraft { get; set; }

	public Layer layerInfo { get; set; }
}
