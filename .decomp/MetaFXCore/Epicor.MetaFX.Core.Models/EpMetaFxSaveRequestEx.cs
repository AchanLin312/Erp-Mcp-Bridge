using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Configurator;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxSaveRequestEx
{
	public string id { get; set; }

	public string commentText { get; set; }

	public string deviceType { get; set; }

	public string userName { get; set; }

	public string viewType { get; set; }

	public JObject layout { get; set; }

	public JObject classicLayout { get; set; }

	public JArray orphans { get; set; }

	public JArray pages { get; set; }

	public JArray rules { get; set; }

	public JArray events { get; set; }

	public JArray tools { get; set; }

	public JArray dataviews { get; set; }

	public Layer layerInfo { get; set; }

	public string countryGroupCode { get; set; }

	public string countryCode { get; set; }

	public Dictionary<string, JToken> serializedEvents { get; set; }

	public string applicationType { get; set; }

	public string subApplicationType { get; set; }

	public JArray properties { get; set; }

	public ConfiguratorInfo configuratorInfo { get; set; }

	public int uxAppVersion { get; set; }
}
