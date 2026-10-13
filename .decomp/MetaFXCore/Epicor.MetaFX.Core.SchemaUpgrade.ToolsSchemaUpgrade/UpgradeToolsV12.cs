using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.ToolsSchemaUpgrade;

internal class UpgradeToolsV12 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public List<ToolV12> tools { get; set; }

		public JArray patch { get; set; }
	}

	public class ToolV12
	{
		public string id { get; set; }

		public string parentId { get; set; }

		public string type { get; set; }

		public string text { get; set; }

		public string icon { get; set; }

		public bool disabled { get; set; }

		public string epBinding { get; set; }
	}

	public UpgradeToolsV12(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.2/tools.json";
		root.contentVersion = "1.2";
		root.imports = schema["imports"];
		root.tools = UpgradeTools(schema["tools"]);
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private List<ToolV12> UpgradeTools(dynamic oldSchema)
	{
		List<ToolV12> list = new List<ToolV12>();
		foreach (dynamic item2 in oldSchema)
		{
			ToolV12 toolV = new ToolV12();
			toolV.id = item2.id;
			toolV.parentId = item2.parentId;
			toolV.type = item2.type;
			toolV.text = item2.text;
			toolV.icon = item2.icon;
			toolV.disabled = item2.disabled;
			toolV.epBinding = $"sysTools.{(object?)item2.id}";
			ToolV12 item = toolV;
			list.Add(item);
		}
		return list;
	}
}
