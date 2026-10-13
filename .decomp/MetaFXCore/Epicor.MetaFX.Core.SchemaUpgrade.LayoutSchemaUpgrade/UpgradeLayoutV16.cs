using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV16 : UpgradeTarget
{
	private List<string> ConvertedPanelCardControls = new List<string>();

	private const string SOURCE_TYPE_ID = "sourceTypeId";

	private const string TARGET_TYPE_ID = "targetTypeId";

	private readonly Dictionary<string, Action<JObject>> convertByType;

	private JToken _layoutComponents;

	private JObject _hierarchicalLayout;

	public UpgradeLayoutV16(ILogger logger)
		: base(logger)
	{
		convertByType = new Dictionary<string, Action<JObject>>
		{
			{ "metafx-panel-card-classic", ConvertPanelCardClassic },
			{ "metafx-panel-card-grid", RemoveGridColumnID },
			{ "erp-grid", RemoveGridColumnID },
			{ "metafx-panel-card", AddFlexBasisToPanelCardDivs }
		};
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.6/layout.json";
		root.contentVersion = "1.6";
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
		AddFlexBasis(oldLayout);
		FlattenLayout flattenLayout = new FlattenLayout();
		_hierarchicalLayout = flattenLayout.ConvertToHierarchicalStructure(oldLayout, addEmptyComponents: false);
		if (!oldLayout.HasValues)
		{
			return oldLayout;
		}
		_layoutComponents = oldLayout["components"];
		if (_layoutComponents != null)
		{
			foreach (JObject item in (IEnumerable<JToken>)_layoutComponents)
			{
				ConvertComponentSpecificProperties(item);
			}
		}
		RemoveComponentsFromFlattenTree(oldLayout);
		AddDatasetId(oldLayout);
		return oldLayout;
	}

	private void ConvertComponentSpecificProperties(JObject component)
	{
		string text = ((!string.IsNullOrEmpty(component["sourceTypeId"]?.ToString())) ? component["sourceTypeId"].ToString() : component["targetTypeId"]?.ToString());
		if (text != null && convertByType.Keys.Contains(text))
		{
			convertByType[text ?? string.Empty](component);
		}
	}

	private void ConvertPanelCardClassic(JObject panelCard)
	{
		if (panelCard["sourceTypeId"].ToString() == "metafx-panel-card-classic")
		{
			if (panelCard["sourceTypeId"] != null)
			{
				panelCard.Remove("sourceTypeId");
			}
			panelCard.Add("targetTypeId", "metafx-panel-card");
			JObject jObject = panelCard["model"] as JObject;
			if (jObject["labelText"] != null)
			{
				jObject.Add("title", jObject["labelText"]);
				jObject.Remove("labelText");
			}
			jObject.Add("expandMore", false);
			jObject.Add("enableActionButton", false);
			jObject.Add("headerComponents", new JArray());
			jObject.Add("expandedHeaderComponents", new JArray());
			jObject.Add("extensionComponents", new JArray());
			jObject.Add("footer", new JObject());
			jObject.Add("actionData", new JArray());
			JToken panelCardChildComponent = GetPanelCardChildComponent(panelCard["id"].ToString());
			jObject.Add("bodyComponents", panelCardChildComponent);
			if (panelCardChildComponent != null)
			{
				AddPanelCardChildComponent(panelCardChildComponent as JArray);
			}
		}
	}

	private JToken GetPanelCardChildComponent(string key)
	{
		_ = _layoutComponents;
		return _hierarchicalLayout["components"].FirstOrDefault((JToken x) => x["id"].ToString() == key)?["components"];
	}

	private void AddPanelCardChildComponent(JArray layoutTree)
	{
		foreach (JToken item in new FlattenLayout().FlattenComponentTree(layoutTree, null))
		{
			ConvertedPanelCardControls.Add(item["id"].ToString());
		}
	}

	private void RemoveComponentsFromFlattenTree(JObject _layout)
	{
		foreach (string key in ConvertedPanelCardControls)
		{
			JToken jToken = _layout["components"];
			JToken jToken2 = jToken.Where((JToken x) => x["id"].ToString() == key).First();
			if (jToken2 != null)
			{
				(jToken as JArray).Remove(jToken2);
			}
		}
	}

	private void RemoveGridColumnID(JObject grid)
	{
		if (grid["targetTypeId"]?.ToString() == "metafx-panel-card-grid")
		{
			RemoveID(grid["model"]["gridModel"] as JObject);
		}
		else if (grid["sourceTypeId"]?.ToString() == "erp-grid")
		{
			RemoveID(grid["model"] as JObject);
		}
	}

	private void RemoveID(JObject grid)
	{
		if (grid == null || grid["columns"] == null)
		{
			return;
		}
		foreach (JObject item in (IEnumerable<JToken>)grid["columns"])
		{
			if (item["id"] != null)
			{
				item.Remove("id");
			}
		}
	}

	private void AddDatasetId(JObject layout)
	{
		if (layout["navigation"] != null && layout["navigation"]["dataSetId"] == null)
		{
			(layout["navigation"] as JObject).Add("dataSetId", "%datasetId%");
		}
	}

	private void AddFlexBasis(JObject oldLayout)
	{
		if (oldLayout["components"] != null)
		{
			(from c in oldLayout["components"].ToList()
				where c["sourceTypeId"]?.ToString() == "metafx-div"
				select c).ToList().ForEach(delegate(JToken div)
			{
				AddFlexBasisToDiv(div as JObject);
			});
		}
	}

	private void AddFlexBasisToDiv(JObject div)
	{
		if (div["model"] != null && div["model"]["flexBasis"] == null)
		{
			(div["model"] as JObject).Add("flexBasis", "50%");
		}
		if (div["components"] == null)
		{
			return;
		}
		foreach (JToken item in (IEnumerable<JToken>)div["components"])
		{
			if (item["sourceTypeId"]?.ToString() == "metafx-div")
			{
				AddFlexBasisToDiv(item as JObject);
			}
		}
	}

	private void AddFlexBasisToPanelCardDivs(JObject panelCard)
	{
		JToken jToken = panelCard["model"];
		if (jToken != null)
		{
			AddFlexBasisToSubComponents(jToken["bodyComponents"]);
			AddFlexBasisToSubComponents(jToken["headerComponents"]);
			AddFlexBasisToSubComponents(jToken["extensionComponents"]);
			AddFlexBasisToSubComponents(jToken["expandedHeaderComponents"]);
		}
	}

	private void AddFlexBasisToSubComponents(JToken components)
	{
		if (components == null)
		{
			return;
		}
		foreach (JToken item in (IEnumerable<JToken>)components)
		{
			if (item["sourceTypeId"] != null && item["sourceTypeId"].ToString() == "metafx-div")
			{
				AddFlexBasisToDiv(item as JObject);
			}
		}
	}
}
