using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.RowRuleSchemaUpgrade;

internal class UpgradeRowRuleV11 : UpgradeTarget
{
	private class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray rules { get; set; }

		public JArray patch { get; set; }
	}

	public UpgradeRowRuleV11(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.1/rules.json";
		root.contentVersion = "1.1";
		root.imports = schema["imports"];
		root.rules = schema["rules"];
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}
}
