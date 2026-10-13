using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.SearchSchemaUpgrade;

internal class UpgradeSearchV40 : UpgradeTarget
{
	private class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public dynamic search { get; set; }
	}

	public UpgradeSearchV40(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		RootV30 rootV = new RootV30();
		rootV.schema = "https://schemas.developer.epicor.com/schemas/v4.0/search.json";
		rootV.contentVersion = "4.0";
		rootV.imports = schema["imports"];
		rootV.search = (object)schema["search"];
		rootV.patch = schema["patch"];
		RootV30 root = rootV;
		return Deserialize(root);
	}
}
