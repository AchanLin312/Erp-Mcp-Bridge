using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.ToolsSchemaUpgrade;

internal class UpgradeToolsV11 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public List<ToolV11> tools { get; set; }

		public JArray patch { get; set; }
	}

	public class ToolV11
	{
		public string id { get; set; }

		public string parentId { get; set; }

		public string type { get; set; }

		public string text { get; set; }

		public string icon { get; set; }

		public bool disabled { get; set; }
	}

	public UpgradeToolsV11(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.1/tools.json";
		root.contentVersion = "1.1";
		root.imports = schema["imports"];
		root.tools = UpgradeTools(schema["tools"]);
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private List<ToolV11> UpgradeTools(dynamic oldSchema)
	{
		List<ToolV11> list = new List<ToolV11>();
		foreach (dynamic item2 in oldSchema)
		{
			ToolV11 toolV = new ToolV11();
			toolV.id = item2.id;
			toolV.parentId = "epView";
			toolV.type = item2.type;
			toolV.text = item2.text;
			toolV.icon = item2.icon;
			toolV.disabled = item2.disabled;
			ToolV11 item = toolV;
			list.Add(item);
		}
		return list;
	}
}
