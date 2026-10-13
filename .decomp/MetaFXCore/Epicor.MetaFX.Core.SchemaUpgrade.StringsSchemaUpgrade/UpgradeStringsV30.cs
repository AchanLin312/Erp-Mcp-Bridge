using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.StringsSchemaUpgrade;

internal class UpgradeStringsV30 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JObject strings { get; set; }
	}

	public ILogger _logger { get; set; }

	public UpgradeStringsV30(ILogger logger)
		: base(logger)
	{
		_logger = logger;
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v3.0/strings.json";
		root.contentVersion = "3.0";
		root.imports = schema["imports"];
		root.strings = schema["strings"];
		Root root2 = root;
		return Deserialize(root2);
	}
}
