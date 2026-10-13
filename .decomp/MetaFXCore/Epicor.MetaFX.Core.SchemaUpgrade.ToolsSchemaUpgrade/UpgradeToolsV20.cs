using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.ToolsSchemaUpgrade;

internal class UpgradeToolsV20 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray tools { get; set; }
	}

	public UpgradeToolsV20(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.0/tools.json";
		root.contentVersion = "2.0";
		root.imports = schema["imports"];
		root.tools = schema["tools"];
		Root root2 = root;
		return Deserialize(root2);
	}
}
