using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV20(ILogger logger) : UpgradeTarget(logger)
{
	private const string MODEL = "model";

	private const string TARGET_TYPE_ID = "targetTypeId";

	private const string SOURCE_TYPE_ID = "sourceTypeId";

	private const string TOP_ROW_TEMPLATE = "topRowTemplate";

	private const string PANEL_CARD = "metafx-panel-card";

	private const string PANEL_CARD_GRID = "metafx-panel-card-grid";

	private const string EXPAND_MORE = "expandMore";

	private const string FOOTER = "footer";

	private const string SHOW_MORE = "showMore";

	private const string NAV_LINK = "navLinkModel";

	private const string BODY_COMPONENTS = "bodyComponents";

	private const string EXTENSION_COMPONENTS = "extensionComponents";

	private const string EXPANDED_HEADER_COMPONENTS = "expandedHeaderComponents";

	private const string HEADER_COMPONENTS = "headerComponents";

	private const string COMPONENTS = "components";

	private const string NAVIGATION = "navigation";

	private const string DATA = "data";

	private const string TOP = "top";

	private const string ID = "id";

	private const string LABELTEXT = "labelText";

	private const string PAGE = "page";

	private const string SELECTED = "selected";

	private readonly Dictionary<string, string> targetTypeIdComponents = new Dictionary<string, string>
	{
		{ "METAFX_PANEL_CARD", "metafx-panel-card" },
		{ "METAFX_PANEL_CARD_GRID", "metafx-panel-card-grid" },
		{ "EP_LABEL", "ep-label" },
		{ "EP_SEARCH_CHIP_SELECTION", "ep-search-chip-selection" },
		{ "EP_NAV_LINK", "ep-nav-link" },
		{ "EP_ROW_NAVIGATOR", "ep-row-navigator" }
	};

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
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.0/layout.json";
		root.contentVersion = "2.0";
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
				UpdateModel((JObject)item);
				UpdateType((JObject)item);
				UpdateChildComponents((JObject)item);
			}
		}
		if (oldLayout["navigation"] != null)
		{
			JToken component = oldLayout["navigation"];
			UpdateNavigation(component);
		}
		ConvertToTabStructureData(ref oldLayout);
		return oldLayout;
	}

	private void ConvertToTabStructureData(ref JObject oldLayout)
	{
		if (oldLayout.ToString().Contains("metafx-tab"))
		{
			JArray components = oldLayout["components"] as JArray;
			ConvertTabComponents(ref components);
			oldLayout["components"] = components;
		}
	}

	private void UpdateNavigation(JToken Component)
	{
		if (Component["topRowTemplate"] == null)
		{
			return;
		}
		foreach (JToken item in (IEnumerable<JToken>)Component["topRowTemplate"])
		{
			UpdateType((JObject)item);
		}
	}

	private void UpdateChildComponents(JObject Component)
	{
		if (Component["model"] is JObject jObject)
		{
			if (jObject["headerComponents"] != null)
			{
				JToken components = jObject["headerComponents"];
				UpdateHeaderAndExpandedHeaderComponents(components);
			}
			if (jObject["expandedHeaderComponents"] != null)
			{
				JToken components2 = jObject["expandedHeaderComponents"];
				UpdateHeaderAndExpandedHeaderComponents(components2);
			}
			if (jObject["bodyComponents"] != null)
			{
				JToken components3 = jObject["bodyComponents"];
				UpdateBodyAndExtensionComponents(components3);
			}
			if (jObject["extensionComponents"] != null)
			{
				JToken components4 = jObject["extensionComponents"];
				UpdateBodyAndExtensionComponents(components4);
			}
		}
	}

	private void UpdateHeaderAndExpandedHeaderComponents(JToken Components)
	{
		foreach (JToken item in (IEnumerable<JToken>)Components)
		{
			UpdateType((JObject)item);
		}
	}

	private void UpdateBodyAndExtensionComponents(JToken Components)
	{
		foreach (JToken item in (IEnumerable<JToken>)Components)
		{
			if (item["components"] != null)
			{
				foreach (JToken item2 in (IEnumerable<JToken>)item["components"])
				{
					if (item2["components"] != null)
					{
						foreach (JToken item3 in (IEnumerable<JToken>)item2["components"])
						{
							if (item3["components"] != null)
							{
								UpdateBodyAndExtensionComponents(item3["components"]);
							}
							if (item3["targetTypeId"]?.ToString() == "metafx-panel-card" || item3["targetTypeId"]?.ToString() == "metafx-panel-card-grid")
							{
								UpdateChildComponents((JObject)item3);
							}
							UpdateType((JObject)item3);
						}
					}
					if (item2["targetTypeId"]?.ToString() == "metafx-panel-card" || item2["targetTypeId"]?.ToString() == "metafx-panel-card-grid")
					{
						UpdateChildComponents((JObject)item2);
					}
					UpdateType((JObject)item2);
				}
			}
			if (item["targetTypeId"]?.ToString() == "metafx-panel-card" || item["targetTypeId"]?.ToString() == "metafx-panel-card-grid")
			{
				UpdateChildComponents((JObject)item);
			}
			UpdateType((JObject)item);
		}
	}

	private void UpdateModel(JObject Component)
	{
		JObject jObject = Component["model"] as JObject;
		if (Component["targetTypeId"]?.ToString() == "metafx-panel-card")
		{
			if (jObject["expandMore"] != null)
			{
				jObject.Remove("expandMore");
			}
			if (jObject["footer"] != null)
			{
				JObject jObject2 = jObject["footer"] as JObject;
				if (jObject2["showMore"] != null)
				{
					JProperty value = new JProperty("fullScreenButtonVisible", jObject2["showMore"]);
					jObject2.Property("showMore").Replace(value);
					jObject.Property("footer").Replace(value);
				}
			}
		}
		if (Component["targetTypeId"]?.ToString() == "metafx-panel-card-grid" && jObject["footer"] != null)
		{
			JObject jObject3 = jObject["footer"] as JObject;
			if (jObject3["showMore"] != null)
			{
				JProperty value2 = new JProperty("fullScreenButtonVisible", jObject3["showMore"]);
				jObject3.Property("showMore").Replace(value2);
				jObject.Property("footer").Replace(value2);
			}
		}
	}

	private void UpdateType(JObject Component)
	{
		foreach (KeyValuePair<string, string> targetTypeIdComponent in targetTypeIdComponents)
		{
			if (Component["targetTypeId"]?.ToString() == targetTypeIdComponent.Value)
			{
				if (Component["sourceTypeId"]?.ToString() == "" || Component["sourceTypeId"]?.ToString() == targetTypeIdComponent.Value)
				{
					Component.Remove("sourceTypeId");
				}
				JProperty value = new JProperty("sourceTypeId", targetTypeIdComponent.Value);
				Component.Property("targetTypeId").Replace(value);
			}
		}
	}

	private static JArray ConvertTabComponents(ref JArray components)
	{
		List<JObject> list = new List<JObject>();
		int num = 0;
		if (components != null)
		{
			int num2 = components.Count((JToken x) => x["sourceTypeId"].ToString() == "metafx-tab");
			foreach (JObject item in components.ToList())
			{
				num++;
				if (item["sourceTypeId"] != null && item["sourceTypeId"].ToString() == "metafx-tab")
				{
					item["sourceTypeId"] = "metafx-tabstrip";
					list.Add(item);
					if (num == num2 && list.Any())
					{
						GenerateTabComponent(list, item);
					}
					else
					{
						item.Remove();
					}
				}
			}
			if (!components.Any())
			{
				return null;
			}
			return components;
		}
		return null;
	}

	private static JObject GenerateTabComponent(List<JObject> components, JObject component)
	{
		new JObject();
		List<TabDataComponent> tabDataComponents = new List<TabDataComponent>();
		components.ToList().ForEach(delegate(JObject item)
		{
			_ = item["components"];
			TabDataComponent item2 = new TabDataComponent
			{
				id = item["id"].ToString(),
				title = item["model"]["labelText"].ToString(),
				page = System.Convert.ToString(item["model"]["page"]),
				selected = (item["model"]["selected"] != null && System.Convert.ToBoolean(item["model"]["selected"]))
			};
			tabDataComponents.Add(item2);
		});
		JObject obj = component["model"] as JObject;
		((JValue)component["model"]["labelText"]).Value = "";
		JArray jArray = JArray.FromObject(tabDataComponents);
		if (jArray != null)
		{
			component["model"]["data"] = jArray;
		}
		List<string> list = new List<string> { "top", "page", "left", "labelText", "selected", "class" };
		foreach (JProperty item3 in obj.Properties().ToList())
		{
			if (list.Contains(item3.Name))
			{
				item3.Remove();
			}
		}
		if (component.Property("components") != null)
		{
			component.Property("components").Remove();
		}
		return component;
	}
}
