using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV30 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray events { get; set; }
	}

	private Dictionary<string, string> EventType = new Dictionary<string, string>();

	public ILogger _logger { get; set; }

	public UpgradeEventV30(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v3.0/events.json";
		root.contentVersion = "3.0";
		root.imports = schema["imports"];
		root.events = schema["events"];
		Root root2 = root;
		return Deserialize(root2);
	}
}
