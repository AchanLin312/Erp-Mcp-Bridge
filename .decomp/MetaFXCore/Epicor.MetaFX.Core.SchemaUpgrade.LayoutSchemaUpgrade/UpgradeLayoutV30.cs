using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV30 : UpgradeTarget
{
	private const string MODEL = "model";

	private const string SOURCE_TYPE_ID = "sourceTypeId";

	private const string COMPONENTS = "components";

	private const string METAFX_DIV = "metafx-div";

	private const string METAFX_COL = "metafx-col";

	private const string CLASS = "class";

	private const string ORIENTATION = "orientation";

	private const string ID = "id";

	private const string FLEXBASICS = "flexBasis";

	private const string PARENTID = "parentId";

	public UpgradeLayoutV30(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		if (schema["layout"] == null)
		{
			return Deserialize(schema);
		}
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v3.0/layout.json";
		root.contentVersion = "3.0";
		root.imports = schema["imports"];
		root.layout = (object)Convert(schema["layout"]);
		root.patch = (object)schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private dynamic Convert(dynamic layout)
	{
		if (layout is JArray)
		{
			foreach (dynamic item in layout)
			{
				Convert(item);
			}
			return layout;
		}
		return Convert(layout);
	}

	private JObject Convert(JObject oldLayout)
	{
		if (!oldLayout.HasValues)
		{
			return oldLayout;
		}
		JToken jToken = oldLayout["components"];
		if (jToken != null)
		{
			foreach (JToken item in (IEnumerable<JToken>)jToken)
			{
				UpdateType((JObject)item);
				UpdateModel((JObject)item, (JArray)jToken);
			}
		}
		return oldLayout;
	}

	private void UpdateType(JObject Component)
	{
		JObject jObject = Component["model"] as JObject;
		if (Component["sourceTypeId"]?.ToString() == "metafx-div" && jObject["class"]?.ToString() == "col")
		{
			Component["sourceTypeId"] = "metafx-col";
		}
	}

	private void UpdateModel(JObject Component, JArray Components)
	{
		JObject jObject = Component["model"] as JObject;
		if (!(Component["sourceTypeId"]?.ToString() == "metafx-div") && !(Component["sourceTypeId"]?.ToString() == "metafx-col"))
		{
			return;
		}
		if (string.IsNullOrEmpty(jObject["id"]?.ToString()) && !string.IsNullOrEmpty(Component["id"]?.ToString()))
		{
			jObject["id"] = Component["id"];
		}
		if (jObject["class"]?.ToString() == "col")
		{
			jObject.Remove("class");
			if (Components.Count((JToken x) => x["parentId"].ToString() == Component["parentId"].ToString()) == 1)
			{
				jObject["flexBasis"] = "100%";
			}
		}
		if (jObject["class"]?.ToString() == "row")
		{
			jObject.Remove("flexBasis");
			jObject.Remove("class");
		}
		if (jObject["orientation"]?.ToString() == "vertical")
		{
			jObject.Remove("orientation");
		}
	}
}
