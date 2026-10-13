using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models._4xLayoutUpgrade;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core._4xLayoutUpgrade;

public class Converter
{
	private List<string> controlsToSkip = new List<string> { "Panel", "GroupBox" };

	public List<object> ConvertTopControls(List<JToken> controlTokens, List<JToken> labelControls)
	{
		List<object> list = new List<object>();
		MetaFxComponent<MetaFxDiv> metaFxComponent = CreateRow(null);
		metaFxComponent.Components = null;
		MetaFxComponent<MetaFxDiv> metaFxComponent2 = CreateColumn(metaFxComponent.Id);
		metaFxComponent2.Components = null;
		list.Add(metaFxComponent);
		list.Add(metaFxComponent2);
		foreach (JToken controlToken in controlTokens)
		{
			RemoveAttributes(controlToken);
			controlToken["parentId"] = metaFxComponent2.Id;
			if (labelControls != null)
			{
				MergeLabel(controlToken, labelControls);
			}
			list.Add(controlToken);
		}
		foreach (JToken labelControl in labelControls)
		{
			RemoveAttributes(labelControl);
			labelControl["parentId"] = metaFxComponent2.Id;
			if (labelControls != null)
			{
				MergeLabel(labelControl, labelControls);
			}
			list.Add(labelControl);
		}
		return list;
	}

	public object Convert(JToken tabToken, List<JToken> allTokens, out bool showWarning)
	{
		showWarning = false;
		List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> list = GetAllGroups(tabToken, allTokens).ToList();
		List<JToken> allControls = GetAllControls(list);
		if (!allControls.Any() || !allControls.Exists((JToken tk) => tk["sourceTypeId"]?.ToString() != "metafx-tabstrip"))
		{
			return null;
		}
		showWarning = true;
		if (string.IsNullOrEmpty(tabToken["pageName"]?.ToString()))
		{
			return null;
		}
		if (allControls.Count == 1 && allControls[0]["sourceTypeId"]?.ToString() == "erp-grid")
		{
			MetaFxComponent<Grid> metaFxComponent = allControls[0].ToObject<MetaFxComponent<Grid>>();
			metaFxComponent.ParentId = null;
			return ConvertToGrid(metaFxComponent);
		}
		return ConvertToPanelCard(tabToken, allTokens, list);
	}

	private MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCard> ConvertToPanelCard(JToken tabToken, List<JToken> allTokens, List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> groups)
	{
		MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCard> metaFxComponent = new MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCard>();
		metaFxComponent.Id = tabToken["panelCardId"]?.ToString() ?? tabToken["id"]?.ToString();
		metaFxComponent.ParentId = null;
		metaFxComponent.SourceTypeId = "metafx-panel-card";
		metaFxComponent.Model = new Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCard();
		metaFxComponent.Model.Id = metaFxComponent.Id;
		metaFxComponent.Model.Guid = Guid.NewGuid().ToString();
		metaFxComponent.Model.Title = TrimLabel(tabToken["title"]?.ToString());
		foreach (Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group group in groups)
		{
			AddGroupControls(allTokens, metaFxComponent.Model.BodyComponents, group, metaFxComponent.Id);
		}
		return metaFxComponent;
	}

	private void AddGroupControls(List<JToken> tokens, List<object> components, Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group group, string parentId)
	{
		List<JToken> list = MergeControls(group.Controls).ToList();
		MetaFxComponent<MetaFxDiv> metaFxComponent = CreateRow(parentId);
		MetaFxComponent<MetaFxDiv> metaFxComponent2 = CreateColumn(metaFxComponent.Id, group.Label);
		metaFxComponent.Components.Add(metaFxComponent2);
		components.Add(metaFxComponent);
		foreach (JToken item in list)
		{
			bool flag = false;
			RemoveAttributes(item);
			switch (item["sourceTypeId"]?.ToString())
			{
			case "erp-grid":
			{
				MetaFxComponent<Grid> grid = item.ToObject<MetaFxComponent<Grid>>();
				MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCardGrid> metaFxComponent3 = ConvertToGrid(grid);
				metaFxComponent3.ParentId = metaFxComponent2.Id;
				JToken jToken = tokens.FirstOrDefault((JToken tk) => tk["id"]?.ToString() == parentId && tk["sourceTypeId"]?.ToString() == "metafx-tabstrip");
				metaFxComponent3.Model.DisablePanelCardStyle = jToken != null;
				metaFxComponent2.Components.Add(metaFxComponent3);
				break;
			}
			case "erp-combo-box":
				if (item["model"] != null)
				{
					if (!string.IsNullOrEmpty(item["model"]["appDesignerComboType"]?.ToString()))
					{
						item["model"]["appDesignerComboType"] = "../" + item["model"]["appDesignerComboType"];
					}
					if (!string.IsNullOrEmpty(item["model"]["$ref"]?.ToString()))
					{
						item["model"]["$ref"] = "../" + item["model"]["$ref"];
					}
				}
				flag = true;
				break;
			default:
				flag = true;
				break;
			case "metafx-tabstrip":
				break;
			}
			if (flag)
			{
				item["parentId"] = metaFxComponent2.Id;
				metaFxComponent2.Components.Add(JToken.FromObject(item));
			}
		}
	}

	private MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCardGrid> ConvertToGrid(MetaFxComponent<Grid> grid)
	{
		MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCardGrid> metaFxComponent = new MetaFxComponent<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCardGrid>();
		metaFxComponent.Id = grid.Id;
		metaFxComponent.ParentId = grid.ParentId;
		metaFxComponent.SourceTypeId = "metafx-panel-card-grid";
		if (grid.Model != null)
		{
			metaFxComponent.Model = new Epicor.MetaFX.Core.Models._4xLayoutUpgrade.MetaFxPanelCardGrid();
			metaFxComponent.Model.Id = grid.Model.Id;
			metaFxComponent.Model.Guid = grid.Model.Guid;
			metaFxComponent.Model.GridModel = grid.Model;
			metaFxComponent.Model.Title = grid.Model.Title;
		}
		return metaFxComponent;
	}

	private void RemoveAttributes(JToken obj)
	{
		if (obj["model"] == null)
		{
			return;
		}
		JToken? jToken = obj["model"];
		List<JToken> list = new List<JToken>();
		foreach (JToken item in jToken.Children())
		{
			if (item is JProperty jProperty && Enumerable.Contains(Constants.ATTRIBUTES_TO_REMOVE, jProperty.Name))
			{
				list.Add(item);
			}
		}
		foreach (JToken item2 in list)
		{
			item2.Remove();
		}
	}

	private void PopulateGroups(JToken obj, List<JToken> tokens, List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> groups)
	{
		if (string.IsNullOrEmpty(obj["id"]?.ToString()))
		{
			return;
		}
		string id = obj["id"].ToString();
		IOrderedEnumerable<JToken> orderedEnumerable = tokens.Where((JToken tk) => tk["parentId"]?.ToString() == id).OrderBy(delegate(JToken tk)
		{
			string value = tk["model"]?["top"]?.ToString();
			return (!string.IsNullOrEmpty(value)) ? System.Convert.ToInt32(value) : 0;
		}).ThenBy(delegate(JToken tk)
		{
			string value = tk["model"]?["left"]?.ToString();
			return (!string.IsNullOrEmpty(value)) ? System.Convert.ToInt32(value) : 0;
		});
		List<JToken> list = new List<JToken>();
		foreach (JToken item2 in orderedEnumerable)
		{
			string item = item2["sourceTypeId"]?.ToString();
			if (controlsToSkip.Contains(item))
			{
				list.Add(item2);
				continue;
			}
			Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group obj2 = groups.FirstOrDefault((Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group grp) => grp.Id == id);
			if (obj2 == null)
			{
				obj2 = new Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group
				{
					Id = id,
					Label = TrimLabel(obj["model"]?["labelText"]?.ToString()),
					ParentId = GetGroupParentId(obj["parentId"]?.ToString(), tokens, groups),
					Controls = new List<JToken> { item2 }
				};
				groups.Add(obj2);
			}
			else
			{
				obj2.Controls.Add(item2);
			}
		}
		foreach (JToken item3 in list)
		{
			PopulateGroups(item3, tokens, groups);
		}
	}

	private string GetGroupParentId(string id, List<JToken> tokens, List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> groups)
	{
		Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group obj;
		while (true)
		{
			JToken parentControl = tokens.FirstOrDefault((JToken tk) => tk["id"]?.ToString() == id);
			if (parentControl == null)
			{
				return string.Empty;
			}
			obj = groups.FirstOrDefault((Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group grp) => grp.Id == parentControl["id"]?.ToString());
			if (obj != null)
			{
				break;
			}
			id = parentControl["parentId"]?.ToString();
		}
		return obj.Id;
	}

	private List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> GetAllGroups(JToken obj, List<JToken> tokens)
	{
		List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> list = new List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group>();
		PopulateGroups(obj, tokens, list);
		return list;
	}

	private List<JToken> MergeControls(List<JToken> groupControls)
	{
		List<JToken> list = groupControls.Where((JToken ctrl) => ctrl["sourceTypeId"]?.ToString() != "ep-label").ToList();
		List<JToken> list2 = groupControls.Where((JToken ctrl) => ctrl["sourceTypeId"]?.ToString() == "ep-label").ToList();
		foreach (JToken item in list)
		{
			MergeLabel(item, list2);
		}
		foreach (JToken item2 in list2)
		{
			list.Add(item2);
		}
		return list;
	}

	private void MergeLabel(JToken control, List<JToken> labelControls)
	{
		if (!string.IsNullOrEmpty(control["model"]?["epiLabel"]?.ToString()))
		{
			JToken jToken = labelControls.FirstOrDefault((JToken ctrl) => ctrl["id"]?.ToString() == control["model"]["epiLabel"].ToString());
			if (jToken != null && control["model"] != null)
			{
				control["model"]["labelText"] = TrimLabel(jToken["model"]?["labelText"]?.ToString());
				control["model"]["epiLabel"] = string.Empty;
				labelControls.Remove(jToken);
			}
		}
	}

	private List<JToken> GetAllControls(List<Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group> groupControls)
	{
		List<JToken> list = new List<JToken>();
		foreach (Epicor.MetaFX.Core.Models._4xLayoutUpgrade.Group groupControl in groupControls)
		{
			list.AddRange(groupControl.Controls);
		}
		return list;
	}

	private MetaFxComponent<MetaFxDiv> CreateRow(string parentId)
	{
		return new MetaFxComponent<MetaFxDiv>
		{
			Id = "div-row-" + Guid.NewGuid().ToString().Substring(0, 5),
			SourceTypeId = "metafx-div",
			Model = new MetaFxDiv
			{
				Guid = Guid.NewGuid().ToString(),
				Class = "row",
				Orientation = "vertical"
			},
			Components = new List<object>(),
			ParentId = parentId
		};
	}

	private MetaFxComponent<MetaFxDiv> CreateColumn(string parentId, string labelText = null)
	{
		return new MetaFxComponent<MetaFxDiv>
		{
			Id = "div-col-" + Guid.NewGuid().ToString().Substring(0, 5),
			SourceTypeId = "metafx-div",
			Model = new MetaFxDiv
			{
				Guid = Guid.NewGuid().ToString(),
				Class = "col",
				FlexBasis = "100%",
				LabelText = labelText
			},
			Components = new List<object>(),
			ParentId = parentId
		};
	}

	private string TrimLabel(string labelText)
	{
		return labelText?.TrimEnd(' ', ':');
	}
}
