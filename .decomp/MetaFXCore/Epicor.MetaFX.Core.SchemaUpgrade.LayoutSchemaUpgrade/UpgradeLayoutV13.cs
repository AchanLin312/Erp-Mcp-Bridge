using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV13 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JObject layout { get; set; }

		public dynamic patch { get; set; }
	}

	public UpgradeLayoutV13(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		FlattenLayout flattenLayout = new FlattenLayout();
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.3/layout.json";
		root.contentVersion = "1.3";
		root.imports = null;
		root.layout = ((schema["layout"] != null) ? flattenLayout.ConvertToFlattenLayout(Convert(schema["layout"])) : null);
		root.patch = (object)schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private JObject Convert(JObject oldLayout)
	{
		foreach (JObject item in (IEnumerable<JToken>)oldLayout["components"])
		{
			if (item["type"].ToString() == "Div")
			{
				if (item["visible"] == null)
				{
					item.Add("visible", true);
				}
				if (item["flexBasis"] == null)
				{
					item.Add("flexBasis", "50%");
				}
			}
		}
		if (oldLayout["patch"] != null)
		{
			oldLayout.Property("patch").Remove();
		}
		return oldLayout;
	}
}
