using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV11(ILogger logger) : UpgradeTarget(logger)
{
	private readonly Dictionary<string, string> modifiedProperties = new Dictionary<string, string>
	{
		{ "EpiGuid", "guid" },
		{ "EpiBinding", "epBinding" }
	};

	private readonly List<string> removedProperties = new List<string> { "Row", "Col", "HasUOM", "Format", "Version" };

	public override JObject Upgrade(JObject json)
	{
		return ConvertToCamelCase(UpgradeSchema(json));
	}

	private JObject ConvertToCamelCase(JObject jObject)
	{
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};
		return JObject.Parse(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<ExpandoObject>(jObject.ToString()), settings));
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		schema = UpgradeLayout(schema);
		return Deserialize(schema);
	}

	private JObject UpgradeLayout(dynamic layoutContent)
	{
		JObject jObject = JObject.Parse(layoutContent.ToString());
		if (!(jObject["layout"] is JObject jObject2) || jObject2["Components"] == null)
		{
			return jObject;
		}
		jObject["$schema"] = "https://schemas.developer.epicor.com/schemas/v1.1/layout.json";
		jObject["contentVersion"] = "1.1";
		RemoveProperties(jObject2);
		RenameProperties(jObject2);
		UpgradeControls(jObject2["Components"] as JArray);
		return jObject;
	}

	private void UpgradeControls(JArray oldComponent)
	{
		foreach (JObject item in oldComponent)
		{
			foreach (JProperty item2 in item.Properties().ToList())
			{
				if (removedProperties.Contains(item2.Name))
				{
					item.Remove(item2.Name);
				}
				if (modifiedProperties.Keys.Contains(item2.Name))
				{
					item.Property(item2.Name).Replace(new JProperty(modifiedProperties[item2.Name], item2.Value));
				}
				if (item2.Value.HasValues)
				{
					if (item2.Value.GetType().Name == "JArray")
					{
						UpgradeControls(item2.Value as JArray);
					}
					else if (item2.Value.GetType().Name == "JObject")
					{
						RemoveProperties(item2.Value as JObject);
						RenameProperties(item2.Value as JObject);
					}
				}
			}
		}
	}

	private void RemoveProperties(JObject component)
	{
		foreach (JProperty item in (from p in component.Properties()
			where removedProperties.Contains(p.Name)
			select p).ToList())
		{
			component.Remove(item.Name);
		}
	}

	private void RenameProperties(JObject component)
	{
		foreach (JProperty item in (from p in component.Properties()
			where modifiedProperties.Keys.Contains(p.Name)
			select p).ToList())
		{
			component.Property(item.Name).Replace(new JProperty(modifiedProperties[item.Name], item.Value));
		}
	}
}
