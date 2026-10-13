using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV21 : UpgradeTarget
{
	public UpgradeLayoutV21(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.1/layout.json";
		root.contentVersion = "2.1";
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
		if (oldLayout["prefetch"] != null)
		{
			oldLayout.Property("prefetch").Remove();
		}
		JObject jObject = new FlattenLayout().ConvertToFlattenLayout(oldLayout);
		foreach (JToken item in (IEnumerable<JToken>)jObject["components"])
		{
			RemoveTabIndexFields((JObject)item);
		}
		return jObject;
	}

	private JToken RemoveTabIndexFields(JToken token)
	{
		if (!(token is JContainer jContainer))
		{
			return token;
		}
		List<JToken> list = new List<JToken>();
		foreach (JToken item in jContainer.Children())
		{
			if (item is JProperty { Name: "tabIndex" })
			{
				list.Add(item);
			}
			RemoveTabIndexFields(item);
		}
		using List<JToken>.Enumerator enumerator2 = list.GetEnumerator();
		if (enumerator2.MoveNext())
		{
			enumerator2.Current.Remove();
			return token;
		}
		return token;
	}
}
