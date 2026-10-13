using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.RowRuleSchemaUpgrade;

internal class UpgradeRowRuleV12 : UpgradeTarget
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

	public UpgradeRowRuleV12(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.2/rules.json";
		root.contentVersion = "1.2";
		root.imports = schema["imports"];
		root.rules = UpgradeRules(schema["rules"]);
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private object UpgradeRules(JArray oldSchema)
	{
		foreach (JObject item in oldSchema)
		{
			item["customizable"] = false;
			item["disabled"] = false;
		}
		return oldSchema;
	}
}
