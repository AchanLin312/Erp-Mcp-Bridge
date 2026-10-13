using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV20 : UpgradeTarget
{
	private class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray events { get; set; }

		public JArray patch { get; set; }
	}

	public ILogger _logger { get; set; }

	public UpgradeEventV20(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.0/events.json";
		root.contentVersion = "2.0";
		root.imports = schema["imports"];
		root.events = schema["events"];
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}
}
