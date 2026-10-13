using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV15 : UpgradeTarget
{
	private const string MODEL = "model";

	private const string SOURCE_TYPE_ID = "sourceTypeId";

	private const string TARGET_TYPE_ID = "targetTypeId";

	private readonly Dictionary<string, Action<JObject>> convertByType;

	private readonly Dictionary<string, string> renamedComboProperties = new Dictionary<string, string>
	{
		{ "epiBOName", "svc" },
		{ "displayMember", "textField" },
		{ "valueMember", "valueField" },
		{ "epiSort", "sort" },
		{ "epiColumns", "retrieveColumns" },
		{ "epiTableName", "tableName" },
		{ "epiFilters", "filters" },
		{ "returnObj", "parameterName" }
	};

	private readonly Dictionary<string, string> renamedProperties = new Dictionary<string, string>
	{
		{ "type", "sourceTypeId" },
		{ "epiKeyField", "isKeyField" },
		{ "readOnly", "readonly" },
		{ "enabled", "disabled" },
		{ "visible", "hidden" },
		{ "caption", "labelText" },
		{ "align", "alignment" }
	};

	private readonly List<string> controlsWithTargetTypeId = new List<string> { "SearchChip", "Label", "ep-nav-link", "ep-row-navigator", "metafx-panel-card-grid" };

	public UpgradeLayoutV15(ILogger logger)
		: base(logger)
	{
		convertByType = new Dictionary<string, Action<JObject>>
		{
			{ "OutputFormatComboBox", ConvertOutputFormatCombo },
			{ "SearchChip", ConvertSearchChip },
			{ "erp-combo-box", ConvertCombo },
			{ "PanelCard", ConvertPanelCard },
			{ "erp-radio-button", ConvertErpRadioButton },
			{ "Label", ConvertLabel },
			{ "SplitButton", ConvertSplitButton },
			{ "Div", ConvertDiv },
			{ "Tab", ConvertTab },
			{ "erp-token-date", ConvertErpTokenDate }
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.5/layout.json";
		root.contentVersion = "1.5";
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
		foreach (JObject item in (IEnumerable<JToken>)oldLayout["components"])
		{
			ConvertCommonProperties(item);
			ConvertComponentSpecificProperties(item);
			if (item["componentProps"] != null)
			{
				MovePropertiesFromCurrentToRoot("componentProps", item);
			}
			MovePropertiesToModel(item);
		}
		return oldLayout;
	}

	private void MovePropertiesToModel(JObject component)
	{
		if (component["uniqueName"] != null)
		{
			component.Property("uniqueName").Remove();
		}
		List<string> rootProperties = new List<string> { "id", "parentId", "sourceTypeId", "targetTypeId" };
		List<JProperty> list = (from p in component.Properties()
			where !rootProperties.Contains(p.Name)
			select p).ToList();
		list.Insert(0, new JProperty("id", component["id"]));
		component.Add("model", new JObject(list));
		list.ForEach(delegate(JProperty p)
		{
			if (p.Name != "model" && p.Name != "id" && !IsComment(p))
			{
				component.Remove(p.Name);
			}
		});
	}

	private void ConvertComponentSpecificProperties(JObject component)
	{
		string text = component["sourceTypeId"]?.ToString() ?? component["targetTypeId"]?.ToString();
		if (convertByType.Keys.Contains(text))
		{
			convertByType[text](component);
		}
	}

	private void ConvertCommonProperties(JObject component)
	{
		foreach (JProperty item in component.Properties().ToList())
		{
			if (renamedProperties.Keys.Contains(item.Name))
			{
				if (item.Name == "type" && controlsWithTargetTypeId.Contains(item.Value.ToString()))
				{
					component.Property(item.Name).Replace(new JProperty("targetTypeId", item.Value));
				}
				else
				{
					component.Property(item.Name).Replace(new JProperty(renamedProperties[item.Name], item.Value));
				}
			}
		}
		foreach (JProperty item2 in component.Properties())
		{
			if (item2.Name == "disabled" || item2.Name == "hidden")
			{
				item2.Value = item2.Value.ToString().ToLower() == "false";
			}
		}
	}

	private void ConvertOutputFormatCombo(JObject outputFormatCombo)
	{
		if (outputFormatCombo["componentProps"] != null)
		{
			outputFormatCombo.Remove("componentProps");
		}
		if (outputFormatCombo["appDesignerComboType"] != null)
		{
			outputFormatCombo.Remove("appDesignerComboType");
		}
		if (outputFormatCombo["appDesignerComboOverride"] != null)
		{
			outputFormatCombo.Remove("appDesignerComboOverride");
		}
		if (outputFormatCombo["dbColumn"] != null)
		{
			outputFormatCombo.Remove("dbColumn");
		}
		if (outputFormatCombo["dropDownStyle"] != null)
		{
			outputFormatCombo.Remove("dropDownStyle");
		}
		if (outputFormatCombo["dataSource"] != null)
		{
			outputFormatCombo.Remove("dataSource");
		}
		if (outputFormatCombo["retrieveOnActivate"] != null)
		{
			outputFormatCombo.Remove("retrieveOnActivate");
		}
		if (outputFormatCombo["sourceTypeId"].ToString() == "OutputFormatComboBox")
		{
			outputFormatCombo.Property("sourceTypeId").Value = "erp-print-output-format";
		}
		if (outputFormatCombo["epBindingEnableRouting"] == null)
		{
			outputFormatCombo.Add("epBindingEnableRouting", "");
		}
		if (outputFormatCombo["epBindingReportId"] == null)
		{
			outputFormatCombo.Add("epBindingReportId", "StyleList.ReportID");
		}
	}

	private void ConvertSearchChip(JObject searchChip)
	{
		if (searchChip["targetTypeId"].ToString() == "SearchChip")
		{
			searchChip.Property("targetTypeId").Value = "ep-search-chip-selection";
		}
	}

	private void ConvertLabel(JObject label)
	{
		if (label["targetTypeId"].ToString() == "Label")
		{
			label.Property("targetTypeId").Value = "ep-label";
		}
	}

	private void ConvertSplitButton(JObject splitButton)
	{
		if (splitButton["sourceTypeId"].ToString() == "SplitButton")
		{
			splitButton.Property("sourceTypeId").Value = "erp-button";
		}
		if (splitButton["id"].ToString() == "btnPrint")
		{
			splitButton.Property("id").Value = "printpreview";
		}
		else if (splitButton["id"].ToString() == "btnProcess")
		{
			splitButton.Property("id").Value = "process";
		}
	}

	private void ConvertDiv(JObject div)
	{
		if (div["sourceTypeId"].ToString() == "Div")
		{
			div.Property("sourceTypeId").Value = "metafx-div";
		}
	}

	private void ConvertTab(JObject tab)
	{
		if (tab["sourceTypeId"].ToString() == "Tab")
		{
			tab.Property("sourceTypeId").Value = "metafx-tabstrip";
		}
	}

	private void ConvertCombo(JObject combo)
	{
		if (combo["retrieveOnActivate"] != null)
		{
			combo.Property("retrieveOnActivate").Remove();
		}
		if (combo["componentProps"] != null)
		{
			if (combo["dropDownStyle"] != null)
			{
				combo.Property("dropDownStyle").Remove();
			}
			MovePropertiesFromCurrentToRoot("componentProps", combo);
			if (combo.Property("columns") != null)
			{
				JArray jArray = combo.Property("columns").Value as JArray;
				if (jArray.Count > 0)
				{
					combo.Add("retrieveColumns", jArray);
				}
				combo.Remove("columns");
			}
		}
		else
		{
			if (combo["extendedComboproperties"] == null)
			{
				return;
			}
			JObject jObject = combo["extendedComboproperties"] as JObject;
			foreach (JProperty item in jObject.Properties().ToList())
			{
				if (renamedComboProperties.Keys.Contains(item.Name) && jObject.Property(renamedComboProperties[item.Name]) == null)
				{
					jObject.Property(item.Name).Replace(new JProperty(renamedComboProperties[item.Name], item.Value));
				}
			}
			MovePropertiesFromCurrentToRoot("extendedComboproperties", combo);
			JProperty? jProperty = combo.Property("retrieveColumns");
			object[] content = combo.Property("retrieveColumns").Value.ToString().Split(new char[1] { ',' });
			jProperty.Value = new JArray(content);
			combo.Add("rowFilter", "");
			combo.Add("searchFilter", "");
			combo.Add("isDelimited", true);
		}
	}

	private void ConvertPanelCard(JObject panelCard)
	{
		JProperty jProperty = panelCard.Property("expandPanelBar");
		if (jProperty != null)
		{
			panelCard.Property("expandPanelBar").Replace(new JProperty("expanded", jProperty.Value));
		}
		if (panelCard["sourceTypeId"].ToString() == "PanelCard")
		{
			panelCard.Property("sourceTypeId").Value = "metafx-panel-card-classic";
		}
	}

	private void ConvertErpRadioButton(JObject radioButton)
	{
		JToken value = radioButton.Property("labelText").Value;
		radioButton["labelText"] = "";
		JProperty jProperty = radioButton.Property("value");
		if (jProperty != null && radioButton.Property("data") == null)
		{
			JObject jObject = new JObject();
			jObject.Add("value", jProperty.Value);
			jObject.Add("label", value);
			radioButton.Property("value").Replace(new JProperty("data", new JArray(jObject)));
		}
		else if (radioButton.Property("value") != null)
		{
			radioButton.Property("value").Remove();
		}
	}

	private void ConvertErpTokenDate(JObject component)
	{
		MovePropertiesFromCurrentToRoot("extendedproperties", component);
	}

	private void MovePropertiesFromCurrentToRoot(string currentObject, JObject root)
	{
		JProperty jProperty = root.Property(currentObject);
		if (jProperty == null)
		{
			return;
		}
		foreach (JProperty item in jProperty.Value.Children<JProperty>())
		{
			if (root.Property(item.Name) != null)
			{
				root.Property(item.Name).Remove();
			}
		}
		root.Add(jProperty.Value.Children<JProperty>());
		jProperty.Remove();
	}
}
