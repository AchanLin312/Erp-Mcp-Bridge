using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.EpControls;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Dashboard;

internal class LayoutManager
{
	private readonly DashboardArgs dashboardArgs;

	private readonly DashboardDataSet dashboardDataSet;

	private readonly DashboardDefinition dashboardDefinition;

	private readonly DashboardHelper dashboardHelper;

	private readonly HelperMethods helperMethods;

	private readonly LayoutHelper layoutHelper;

	private readonly Action<MessageDetail> logMessageDetail;

	public GridBox gridBoxSize;

	public Dictionary<string, List<Component>> tabPanelsList = new Dictionary<string, List<Component>>();

	public int tabIndex { get; set; } = 11;

	public LayoutManager(DashboardArgs dashboardArgs, Action<MessageDetail> logMessageDetail)
	{
		this.logMessageDetail = logMessageDetail;
		this.dashboardArgs = dashboardArgs;
		dashboardHelper = dashboardArgs.DashboardHelper;
		helperMethods = dashboardArgs.HelperMethods;
		layoutHelper = dashboardArgs.LayoutHelper;
		dashboardDataSet = dashboardArgs.DashboardDataSet;
		dashboardDefinition = dashboardArgs.DashboardDefinition;
	}

	public JToken GenerateSchema(object layoutTemplate)
	{
		JToken jToken = JToken.FromObject(layoutTemplate);
		JToken jToken2 = jToken["layout"];
		if (jToken2["components"] is JArray jArray)
		{
			jArray.Clear();
		}
		jToken2["name"] = dashboardDefinition.DashBdDef[0].DefinitionID;
		jToken2["caption"] = ((!string.IsNullOrEmpty(dashboardDefinition.DashBdDef[0].Caption)) ? dashboardDefinition.DashBdDef[0].Caption : dashboardDefinition.DashBdDef[0].Description);
		jToken2["namespace"] = "Ice.UIDbd." + dashboardDefinition.DashBdDef[0].DefinitionID;
		if ((string?)jToken2["viewType"] == "Dashboard")
		{
			jToken2["fitToPageMode"] = true;
		}
		return jToken;
	}

	public void CreateFile(object layoutSchema)
	{
		JObject data = dashboardArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(layoutSchema), "layout");
		helperMethods.CreateJsonFile(data, dashboardArgs.OutputPath, "layout.jsonc");
	}

	public void CreateSearchTrackerPageAndGetComponents(DashboardViewArgs dashboardViewArgs, string controlSeq, out List<Component> trackerComponents)
	{
		if (!Directory.Exists(dashboardArgs.OutputPath + "/Pages"))
		{
			Directory.CreateDirectory(dashboardArgs.OutputPath + "/Pages");
		}
		JToken jToken = JToken.FromObject(dashboardArgs.LayoutTemplate);
		JToken? jToken2 = jToken["layout"];
		jToken2["name"] = dashboardHelper.GetSearchableTrackerPageName(dashboardDataSet, dashboardViewArgs.DashboardQuery, dashboardViewArgs.DashboardView);
		jToken2["caption"] = dashboardViewArgs.DashboardView.Caption;
		jToken2["navigation"]["parentPage"] = dashboardDefinition.DashBdDef[0].DefinitionID;
		jToken2["width"] = "500px";
		jToken2["viewType"] = "page";
		CreateSearchTrackerPage(dashboardViewArgs, controlSeq, out trackerComponents);
		jToken2["components"] = layoutHelper.ConvertToJArray(trackerComponents);
		FlattenLayout flattenLayout = new FlattenLayout();
		jToken["layout"] = flattenLayout.ConvertToFlattenLayout(jToken["layout"] as JObject);
		jToken = dashboardArgs.SchemaUpgrade.UpgradeToLatestSchema(jToken as JObject, "layout");
		helperMethods.CreateJsonFile(jToken, dashboardArgs.OutputPath + "/Pages", dashboardHelper.GetSearchableTrackerPageName(dashboardDataSet, dashboardViewArgs.DashboardQuery, dashboardViewArgs.DashboardView) + ".layout.jsonc");
	}

	public void CreateSearchTrackerPage(DashboardViewArgs dashboardViewArgs, string controlSeq)
	{
		CreateSearchTrackerPageAndGetComponents(dashboardViewArgs, controlSeq, out var _);
	}

	internal void CreateSearchTrackerPage(DashboardViewArgs dashboardViewArgs, string controlSeq, out List<Component> trackerComponents)
	{
		DashboardQuery dashboardQuery = dashboardViewArgs.DashboardQuery;
		DashboardView dashboardView = dashboardViewArgs.DashboardView;
		string searchableTrackerPageName = dashboardHelper.GetSearchableTrackerPageName(dashboardDataSet, dashboardViewArgs.DashboardQuery, dashboardViewArgs.DashboardView);
		Component component = layoutHelper.CreatePanelCard(dashboardViewArgs.DashboardView.Caption, searchableTrackerPageName, expandPanelBar: true);
		SetPanelCardBoxData(component, dashboardView, dashboardQuery);
		UpdateTabPanelsList(component, dashboardView);
		trackerComponents = new List<Component> { component };
		Component component2 = layoutHelper.CreateRow("", component.Id, controlSeq);
		int num = 1;
		int num2 = 0;
		List<DashboardViewColumns> list = dashboardViewArgs.DashboardViewColumns.FindAll((DashboardViewColumns dvc) => dvc.IsVisible);
		Component component3 = null;
		double num3 = 100.0 / Math.Ceiling((double)list.Count / 4.0);
		foreach (DashboardViewColumns item in list)
		{
			tabIndex++;
			if (num2++ % 4 == 0)
			{
				component3 = layoutHelper.CreateColumn($"{num3:f0}%", component2.Id, $"{controlSeq}-col{num++}");
				component2.Components.Add(component3);
			}
			QueryField queryField = dashboardHelper.GetQueryField(item.ColumnName, dashboardViewArgs.DynamicQueryDefinition);
			Component component4 = CreateTrackerColumn(dashboardViewArgs.DataViewName, item, isPage: false, queryField?.FieldFormat, component3.Id);
			if (component4 != null)
			{
				component3.Components.Add(component4);
			}
		}
		if (!component2.Components.Contains(component3))
		{
			component2.Components.Add(component3);
		}
		trackerComponents.Add(component2);
		Component component5 = layoutHelper.CreateRow("", component.Id);
		Component component6 = layoutHelper.CreateColumn("100%", component5.Id);
		component5.Components.Add(component6);
		Component component7 = layoutHelper.CreateButtonControl("Reset Search", dashboardHelper.GetResetPageSearchButtonName(dashboardViewArgs), 1, component6.Id);
		component7.Model.TabIndex = tabIndex++;
		component6.Components.Add(component7);
		trackerComponents.Add(component5);
	}

	public List<Component> GenerateTrackerPanel(DashboardViewArgs dashboardViewArgs, string controlSeq)
	{
		List<Component> list = new List<Component>();
		DashboardQuery dashboardQuery = dashboardViewArgs.DashboardQuery;
		DashboardView dashboardView = dashboardViewArgs.DashboardView;
		string caption = (dashboardViewArgs.IsTrackerEmbedGrid ? dashboardQuery.Caption : dashboardView.Caption);
		Component component = layoutHelper.CreatePanelCard(caption, dashboardHelper.GetPanelName(dashboardViewArgs, controlSeq), expandPanelBar: true);
		SetPanelCardBoxData(component, dashboardView, dashboardQuery);
		UpdateTabPanelsList(component, dashboardView);
		list.Add(component);
		CreateTrackerComponents(dashboardViewArgs, component.Id, list, controlSeq);
		return list;
	}

	public List<Component> GenerateChartPanel(DashboardViewArgs dashboardViewArgs, EpAppWidget widgets, int gaugeChartCount)
	{
		List<Component> list = new List<Component>();
		string text = dashboardHelper.GetPanelName(dashboardViewArgs, gaugeChartCount.ToString());
		if (dashboardViewArgs.IsGauge)
		{
			text += $"_gauge{gaugeChartCount}";
		}
		Component component = layoutHelper.CreatePanelCard(dashboardViewArgs.DashboardView.Caption, text, expandPanelBar: true);
		SetPanelCardBoxData(component, dashboardViewArgs.DashboardView, dashboardViewArgs.DashboardQuery);
		UpdateTabPanelsList(component, dashboardViewArgs.DashboardView);
		list.Add(component);
		list.AddRange(layoutHelper.CreateChartPanel(dashboardViewArgs.DashboardView.DasboardChart, component.Id, widgets));
		return list;
	}

	public Component GenerateGridPanel(PanelCardGridArgs panelCardGridArgs, string parentId = null, List<Epicor.MetaFX.Core.Models.Layout.ErpControls.Group> group = null)
	{
		List<DashboardViewArgs> dashboardViewArgsList = panelCardGridArgs.DashboardViewArgsList;
		bool editable = panelCardGridArgs.DashboardViewArgsList.Any((DashboardViewArgs dashboardViewArgs) => dashboardViewArgs.IsUpdatable);
		Component component = new Component
		{
			Id = GetUniqueCompoentID(panelCardGridArgs),
			ParentId = parentId,
			SourceTypeId = "metafx-panel-card-grid",
			Model = new MetaFxPanelCardGrid
			{
				Id = GetUniqueCompoentID(panelCardGridArgs),
				Guid = panelCardGridArgs.ComponentId,
				Hidden = false,
				Title = GetPanelCardGridCaption(panelCardGridArgs),
				Expanded = true,
				AutoFillContainer = true,
				FullScreenButtonVisible = true,
				GridModel = new GridModel
				{
					Height = 350,
					Scrollable = "virtual",
					PageSize = 100,
					Sortable = true,
					Resizable = true,
					Editable = editable,
					ProviderModel = ((dashboardViewArgsList.Count > 1) ? null : CreateProviderModel(panelCardGridArgs, dashboardViewArgsList[0], isDefault: true)),
					Columns = GetGridColumns(panelCardGridArgs),
					Group = group?.ToArray()
				},
				ViewOptions = ((dashboardViewArgsList.Count > 1) ? CreateProviderModels(panelCardGridArgs) : null),
				ActionData = CreateActionData(panelCardGridArgs)
			}
		};
		SetPanelCardBoxData(component, dashboardViewArgsList[0].DashboardView, dashboardViewArgsList[0].DashboardQuery);
		UpdateTabPanelsList(component, dashboardViewArgsList[0].DashboardView);
		return component;
	}

	public Component GeneratePanelCardStack(List<Component> stackedComponentList)
	{
		string text = Guid.NewGuid().ToString();
		List<PanelCardStackItem> list = new List<PanelCardStackItem>();
		foreach (Component stackedComponent in stackedComponentList)
		{
			PanelCardStackItem item = new PanelCardStackItem
			{
				Id = stackedComponent.Model.Guid,
				Name = stackedComponent.Model.Id
			};
			list.Add(item);
		}
		return new Component
		{
			Id = text,
			ParentId = null,
			SourceTypeId = "metafx-panel-card-stack",
			Model = new MetaFxPanelCardStack
			{
				Guid = text,
				Id = "metafx-panel-card-stack-" + text.Substring(0, 5),
				Panels = list,
				Title = "metafx-panel-card-stack-1",
				AutoSize = "container",
				Expanded = true,
				DisableExpandCollapse = true
			}
		};
	}

	private string GetUniqueCompoentID(PanelCardGridArgs panelCardGridArgs)
	{
		string panelCardGridCaption = GetPanelCardGridCaption(panelCardGridArgs);
		return "metaFxPanelCardGrid" + RemoveSpecialCharacters(panelCardGridCaption);
	}

	public string RemoveSpecialCharacters(string str)
	{
		return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
	}

	internal IEnumerable<Component> GenerateWebViewLayout(DashboardURL urlWidget, string parentId = null)
	{
		string text = Guid.NewGuid().ToString();
		return new List<Component>
		{
			new Component
			{
				Id = urlWidget.UrlPanelID,
				ParentId = parentId,
				SourceTypeId = "metafx-panel-card",
				Model = new MetaFxPanelCard
				{
					Id = urlWidget.UrlPanelID,
					Guid = urlWidget.UrlPanelID,
					Title = urlWidget.Caption,
					Hidden = false,
					Expanded = false,
					BodyComponents = new List<Component>()
				}
			},
			new Component
			{
				Id = urlWidget.UrlID,
				ParentId = text,
				SourceTypeId = "erp-website",
				Model = new ErpWebsite
				{
					Guid = Guid.NewGuid().ToString(),
					Id = urlWidget.UrlID,
					WidgetType = "erp-website",
					Capabilities = new List<Capability>
					{
						new Capability
						{
							Id = "IInterpolateUrl",
							Description = "IInterpolateUrl"
						}
					},
					LabelText = string.Empty,
					Title = string.Empty,
					IconClass = "mdi mdi-web-box",
					Url = urlWidget.URLAddress
				}
			},
			new Component
			{
				Id = text,
				ParentId = urlWidget.UrlPanelID,
				SourceTypeId = "metafx-flex-box",
				Model = new MetaFxBox
				{
					Guid = text,
					Id = text
				}
			}
		};
	}

	private string GetPanelCardGridCaption(PanelCardGridArgs panelCardGridArgs)
	{
		if (panelCardGridArgs.DashboardViewArgsList.Count == 1 && panelCardGridArgs.DashboardViewArgsList[0].IsTrackerEmbedGrid)
		{
			return panelCardGridArgs.DashboardViewArgsList[0].DashboardView.Caption;
		}
		if (!string.IsNullOrEmpty(panelCardGridArgs.DashboardQuery.Caption))
		{
			return panelCardGridArgs.DashboardQuery.Caption;
		}
		string result = string.Empty;
		if (panelCardGridArgs.DashboardViewArgsList.Count == 1)
		{
			result = (string.IsNullOrEmpty(panelCardGridArgs.DashboardViewArgsList[0].DashboardView.Caption) ? panelCardGridArgs.DashboardViewArgsList[0].GridCaption : panelCardGridArgs.DashboardViewArgsList[0].DashboardView.Caption);
		}
		return result;
	}

	private void CreateTrackerComponents(DashboardViewArgs dashboardViewArgs, string parentId, List<Component> trackerComponents, string controlSeq)
	{
		List<DashboardViewColumns> dashboardViewColumns = dashboardViewArgs.DashboardViewColumns.Where((DashboardViewColumns col) => col.IsVisible).ToList();
		DashboardViewColumns[] customDashboardViewColumns = GetCustomDashboardViewColumns(dashboardViewArgs);
		string trackerID = dashboardViewArgs.DashboardView.ViewID + controlSeq;
		MetaFxTwoColumn metaFxTwoColumn = layoutHelper.CreateTwoColumnComponent(parentId, trackerID);
		trackerComponents.Add(metaFxTwoColumn.RowComponent);
		trackerComponents.Add(metaFxTwoColumn.Column1);
		trackerComponents.Add(metaFxTwoColumn.Column2);
		int i;
		for (i = 0; i < dashboardViewColumns.Count; i++)
		{
			bool dashboardPrompt = false;
			tabIndex++;
			QueryField queryField = dashboardHelper.GetQueryField(dashboardViewColumns[i].ColumnName, dashboardViewArgs.DynamicQueryDefinition);
			Component component = ((i % 2 == 0) ? metaFxTwoColumn.Column1 : metaFxTwoColumn.Column2);
			if (dashboardViewArgs.IsUpdatable && customDashboardViewColumns != null)
			{
				DashboardViewColumns dashboardViewColumns2 = customDashboardViewColumns.FirstOrDefault((DashboardViewColumns col) => col.ColumnName == dashboardViewColumns[i].ColumnName);
				if (dashboardViewColumns2 != null)
				{
					dashboardPrompt = dashboardViewColumns2.DashboardPrompt;
					dashboardViewColumns[i].DashboardPrompt = dashboardViewColumns2.DashboardPrompt;
				}
			}
			Component component2 = CreateTrackerColumn(dashboardViewArgs.DataViewName, dashboardViewColumns[i], isPage: false, queryField?.FieldFormat, component.Id, dashboardViewArgs.IsUpdatable, dashboardPrompt);
			if (component2 != null)
			{
				trackerComponents.Add(component2);
			}
		}
	}

	private DashboardViewColumns[] GetCustomDashboardViewColumns(DashboardViewArgs dashboardViewArgs)
	{
		DashboardViewOptions dashboardViewOptions = dashboardDataSet.DashboardViewOptions?.FirstOrDefault((DashboardViewOptions qo) => qo.PropertyName == "ViewColumnTable" && qo.QueryID == dashboardViewArgs.DashboardQuery.QueryID && qo.ViewID == dashboardViewArgs.DashboardView.ViewID);
		if (dashboardViewOptions != null)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(dashboardViewOptions.PropertyValue);
			CustomDashboardViewColumns customDashboardViewColumns = JsonConvert.DeserializeObject<CustomDashboardViewColumns>(JsonConvert.SerializeXmlNode(xmlDocument, Newtonsoft.Json.Formatting.None, omitRootObject: true));
			if (customDashboardViewColumns != null && customDashboardViewColumns.DashboardViewColumns != null)
			{
				return customDashboardViewColumns.DashboardViewColumns;
			}
		}
		return null;
	}

	private Component CreateTrackerColumn(string dataViewName, DashboardViewColumns dashboardViewColumn, bool isPage, string format, string parentId, bool isUpdatable = false, bool dashboardPrompt = false)
	{
		Component component;
		if (dashboardViewColumn.DashboardPrompt && !string.IsNullOrEmpty(dashboardViewColumn.RetrieverCombo))
		{
			string[] array = dashboardViewColumn.RetrieverCombo.Split(new char[1] { '~' });
			if (array.Length == 2)
			{
				string text = array[0].ToLowerInvariant();
				if (text.EndsWith("adapter"))
				{
					text = array[0].Remove(text.LastIndexOf("adapter", StringComparison.Ordinal));
				}
				string text2 = text + "." + array[1];
				if (!File.Exists(GetFilePath(Path.Combine(dashboardArgs.RootPath, "shared", "combo", text2, "default"), "combo")))
				{
					logMessageDetail(new MessageDetail
					{
						Message = "Combo " + text2 + " does not exists.",
						Type = "warning"
					});
				}
				string text3 = "../shared/combo/" + text2 + "/default/combo.jsonc#/combo";
				if (isPage)
				{
					text3 = "../" + text3;
				}
				component = CreateComboBox(dashboardViewColumn, dataViewName + "." + dashboardViewColumn.ColumnName, text3, tabIndex, !(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt, parentId);
				component.Model.Disabled = isUpdatable && !dashboardPrompt;
				return component;
			}
			logMessageDetail(new MessageDetail
			{
				Message = "Combo " + dashboardViewColumn.RetrieverCombo + " not found.",
				Type = "warning"
			});
			return null;
		}
		if (dashboardViewColumn.BaseType != null && dashboardViewColumn.BaseType.StartsWith("BAQCombo"))
		{
			string[] array2 = dashboardViewColumn.BaseType.Split(new char[1] { '~' });
			if (array2.Length != 0 && array2.Length == 3)
			{
				string queryId = array2[0].Split(new char[1] { '_' })[1];
				string textField = array2[1];
				string valueField = array2[2];
				component = new Component
				{
					Id = dashboardViewColumn.Caption,
					ParentId = parentId,
					SourceTypeId = "erp-combo-box",
					Model = new ErpComboBox
					{
						Id = dashboardViewColumn.Caption,
						Guid = Guid.NewGuid().ToString(),
						LabelText = dashboardViewColumn.Caption,
						EpBinding = dataViewName + "." + dashboardViewColumn.ColumnName,
						Readonly = (!(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt),
						Hidden = false,
						TabIndex = tabIndex,
						IsKeyField = false,
						Disabled = false,
						QueryId = queryId,
						TextField = textField,
						ValueField = valueField
					}
				};
				component.Model.Disabled = isUpdatable && !dashboardPrompt;
				return component;
			}
		}
		string uniqueName = ((!string.IsNullOrWhiteSpace(dashboardViewColumn.ControlGuid)) ? dashboardViewColumn.ControlGuid : Guid.NewGuid().ToString());
		switch (dashboardViewColumn.DataType.ToLower())
		{
		case "nvarchar":
		case "character":
			component = layoutHelper.CreateTextBoxControl(uniqueName, dashboardViewColumn.Caption, dataViewName + "." + dashboardViewColumn.ColumnName, tabIndex, !(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt, parentId);
			break;
		case "int":
		case "decimal":
		case "integer":
		case "bigint":
			component = layoutHelper.CreateNumericControl(uniqueName, dashboardViewColumn.Caption, dataViewName + "." + dashboardViewColumn.ColumnName, tabIndex, nullable: true, !(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt, format, parentId);
			break;
		case "bit":
		case "logical":
			component = layoutHelper.CreateCheckBoxControl(uniqueName, dashboardViewColumn.Caption, dataViewName + "." + dashboardViewColumn.ColumnName, tabIndex, !(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt, parentId);
			break;
		case "datetime":
		case "date":
		case "datetimeoffset":
			component = layoutHelper.CreateDatePickerControl(uniqueName, dashboardViewColumn.Caption, dataViewName + "." + dashboardViewColumn.ColumnName, tabIndex, !(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt, parentId);
			break;
		default:
			component = layoutHelper.CreateTextBoxControl(Guid.NewGuid().ToString(), dashboardViewColumn.Caption, dataViewName + "." + dashboardViewColumn.ColumnName, tabIndex, !(isUpdatable & dashboardPrompt) && !dashboardViewColumn.DashboardPrompt, parentId);
			break;
		}
		component.Model.Disabled = isUpdatable && !dashboardPrompt;
		return component;
	}

	private List<ProviderModel> CreateProviderModels(PanelCardGridArgs panelCardGridArgs)
	{
		List<ProviderModel> list = new List<ProviderModel>();
		for (int i = 0; i < panelCardGridArgs.DashboardViewArgsList.Count; i++)
		{
			list.Add(CreateProviderModel(panelCardGridArgs, panelCardGridArgs.DashboardViewArgsList[i], i == 0));
		}
		return list;
	}

	private ProviderModel CreateProviderModel(PanelCardGridArgs panelCardGridArgs, DashboardViewArgs dashboardViewArgs, bool isDefault)
	{
		List<BAQWhereExpression> list = new List<BAQWhereExpression>();
		if (dashboardViewArgs.ClientFilter.Any())
		{
			list.AddRange(dashboardViewArgs.ClientFilter);
		}
		if (panelCardGridArgs.ServerFilter.Any())
		{
			list.AddRange(panelCardGridArgs.ServerFilter);
		}
		foreach (List<BAQWhereExpression> searchFilter in panelCardGridArgs.SearchFilters)
		{
			list.AddRange(searchFilter);
		}
		string text = ((!string.IsNullOrEmpty(dashboardViewArgs.DataViewName)) ? dashboardViewArgs.DataViewName : dashboardHelper.GetDataViewName(dashboardDataSet, dashboardViewArgs.DashboardQuery, dashboardViewArgs.DashboardView));
		if (dashboardViewArgs.IsTrackerEmbedGrid)
		{
			text += "_EmbedGrid";
		}
		string description = ((!string.IsNullOrEmpty(dashboardViewArgs.DashboardView.Caption)) ? dashboardViewArgs.DashboardView.Caption : dashboardViewArgs.GridCaption);
		Columns[] viewOptionsColumns = GetViewOptionsColumns(dashboardViewArgs);
		return CreateProviderModel(description, isDefault, text, dashboardViewArgs.DashboardQuery.DynamicQueryID, string.Empty, list, viewOptionsColumns, autoRefreshOnLoad: true);
	}

	private Columns[] GetViewOptionsColumns(DashboardViewArgs dashboardViewArgs)
	{
		List<Columns> list = new List<Columns>();
		int num = 0;
		if (dashboardViewArgs.DashboardView.ViewType == "Grid")
		{
			foreach (DashboardViewColumns dashboardViewColumn in dashboardViewArgs.DashboardViewColumns)
			{
				if (dashboardViewColumn.IsVisible)
				{
					num++;
					list.Add(new Columns
					{
						Field = dashboardViewColumn.ColumnName,
						Title = dashboardViewColumn.Caption,
						Sortable = true,
						Filterable = true,
						Resizable = true,
						Editable = dashboardViewColumn.DashboardPrompt,
						Groupable = true,
						Hidden = !dashboardViewColumn.IsVisible,
						visibleIndex = num
					});
				}
			}
		}
		if (dashboardViewArgs.DashboardViewColumns.Count == list.Count)
		{
			list = null;
		}
		return list?.ToArray();
	}

	private List<ActionData> CreateActionData(PanelCardGridArgs panelCardGridArgs)
	{
		List<ActionData> list = new List<ActionData>();
		if (panelCardGridArgs.DashboardViewArgsList.FindIndex((DashboardViewArgs dl) => dl.AllowAddNew) >= 0)
		{
			list.Add(new ActionData
			{
				Id = dashboardHelper.GetCustomActionButtonName(panelCardGridArgs.DashboardViewArgsList[0], "Add"),
				Description = panelCardGridArgs.DashboardViewArgsList[0].AddNewLabel,
				Icon = "mdi mdi-plus-circle-outline",
				ShowInToolBar = true,
				UserData = new UserData
				{
					Tag = "toolNew",
					EpBinding = panelCardGridArgs.DashboardViewArgsList[0].DataViewName,
					Type = "Control",
					PageLevel = false
				}
			});
		}
		if (panelCardGridArgs.DashboardViewArgsList.FindIndex((DashboardViewArgs dl) => dl.IsUpdatable) >= 0)
		{
			list.Add(new ActionData
			{
				Id = dashboardHelper.GetCustomActionButtonName(panelCardGridArgs.DashboardViewArgsList[0], "Update"),
				Description = "Update",
				Icon = "mdi mdi-content-save",
				UserData = new UserData
				{
					Tag = "Update",
					EpBinding = panelCardGridArgs.DashboardViewArgsList[0].DataViewName,
					Type = "Control",
					PageLevel = false
				}
			});
		}
		if (panelCardGridArgs.SliderDashboardViews.Any())
		{
			foreach (DashboardViewArgs sliderDashboardView in panelCardGridArgs.SliderDashboardViews)
			{
				list.Add(new ActionData
				{
					Id = dashboardHelper.GetLayoutSearchButtonName(sliderDashboardView),
					Description = (string.IsNullOrEmpty(sliderDashboardView.DashboardView.Caption) ? "Advance Search" : sliderDashboardView.DashboardView.Caption),
					Icon = "mdi mdi-magnify"
				});
			}
		}
		foreach (DashboardViewArgs dashboardViewArgs in panelCardGridArgs.DashboardViewArgsList)
		{
			if (dashboardViewArgs.DocumentElement == null || dashboardViewArgs.DocumentElement.ViewButtons == null)
			{
				continue;
			}
			foreach (ViewButtons viewButton in dashboardViewArgs.DocumentElement.ViewButtons)
			{
				list.Add(new ActionData
				{
					Id = dashboardHelper.GetCustomActionButtonName(dashboardViewArgs, viewButton.Action),
					Description = viewButton.Text
				});
			}
		}
		return list;
	}

	private ProviderModel CreateProviderModel(string description, bool isDefault, string epBinding, string baqId, string clientFilter, List<BAQWhereExpression> whereExpressions, Columns[] columns, bool autoRefreshOnLoad)
	{
		return new ProviderModel
		{
			Description = description,
			IsDefault = isDefault,
			ServerPaging = true,
			EpBinding = epBinding,
			BaqId = baqId,
			AutoRefreshOnLoad = autoRefreshOnLoad,
			BaqOptions = new BaqOptions
			{
				Where = clientFilter,
				WhereList = whereExpressions
			},
			Columns = columns
		};
	}

	internal static Columns[] GetGridColumns(PanelCardGridArgs panelCardGridArgs)
	{
		List<Columns> list = new List<Columns>();
		int num = 0;
		foreach (DashboardViewImageColumns dashboardViewImageColumn in panelCardGridArgs.DashboardViewImageColumns)
		{
			list.Add(new Columns
			{
				Field = dashboardViewImageColumn.ColumnName,
				Title = dashboardViewImageColumn.Caption,
				Sortable = true,
				Filterable = false,
				Resizable = true,
				Editable = false,
				Groupable = false,
				ImageName = ((!string.IsNullOrEmpty(dashboardViewImageColumn.ImageName) && dashboardViewImageColumn.ImageName != "(None)") ? dashboardViewImageColumn.ImageName : string.Empty),
				Hidden = false,
				visibleIndex = ((dashboardViewImageColumn.VisibleIndex == 0) ? 1 : dashboardViewImageColumn.VisibleIndex)
			});
		}
		foreach (DashboardViewColumns dashboardViewColumn in panelCardGridArgs.DashboardViewColumns)
		{
			num = getVisibleIndex(++num, list);
			list.Add(new Columns
			{
				Field = dashboardViewColumn.ColumnName,
				Title = dashboardViewColumn.Caption,
				Sortable = true,
				Filterable = true,
				Resizable = true,
				Editable = dashboardViewColumn.DashboardPrompt,
				Groupable = true,
				Hidden = !dashboardViewColumn.IsVisible,
				visibleIndex = num
			});
		}
		foreach (Columns item in list)
		{
			if (item.visibleIndex == -1)
			{
				num = (item.visibleIndex = num + 1);
			}
		}
		return list.OrderBy((Columns m) => m.visibleIndex).ToList().ToArray();
	}

	private static int getVisibleIndex(int index, List<Columns> columns)
	{
		if (columns.Find((Columns i) => i.visibleIndex == index) != null)
		{
			getVisibleIndex(++index, columns);
		}
		return index;
	}

	private Component CreateComboBox(DashboardViewColumns dashboardViewColumn, string epBinding, string comboType, int tabIndex, bool isReadOnly, string parentId = null)
	{
		return new Component
		{
			Id = dashboardViewColumn.Caption,
			ParentId = parentId,
			SourceTypeId = "erp-combo-box",
			Model = new ErpComboBox
			{
				Id = dashboardViewColumn.Caption,
				Guid = Guid.NewGuid().ToString(),
				LabelText = dashboardViewColumn.Caption,
				EpBinding = epBinding,
				Readonly = isReadOnly,
				Hidden = false,
				TabIndex = tabIndex,
				IsKeyField = false,
				Disabled = false,
				AppDesignerComboType = comboType,
				AppDesignerComboOverride = true,
				EpBindingTextColumn = epBinding + "Desc",
				Reference = comboType
			}
		};
	}

	private string GetFileName(string folderPath, string file)
	{
		if (File.Exists(Path.Combine(folderPath, file + ".jsonc")))
		{
			return file + ".jsonc";
		}
		return file + ".json";
	}

	private string GetFilePath(string folderPath, string type)
	{
		return Path.Combine(folderPath, GetFileName(folderPath, type));
	}

	private void SetPanelCardBoxData(Component panelCard, DashboardView view, DashboardQuery dashboardQuery)
	{
		if (gridBoxSize != null)
		{
			string componentPanelSize = (view.TabId.HasValue ? dashboardQuery.PanelSize : view.PanelSize);
			panelCard.BoxData = GenerateComponentBoxData(componentPanelSize, gridBoxSize, view.ScreenLocation);
		}
	}

	private void UpdateTabPanelsList(Component panelCard, DashboardView view)
	{
		if (view.TabId.HasValue)
		{
			panelCard.IsSelectedTab = view.IsSelectedTab;
			panelCard.TabIndex = view.TabIndex;
			if (!view.IsSelectedTab == true)
			{
				panelCard.BoxData = null;
			}
			string key = view.TabId.ToString();
			if (tabPanelsList.TryGetValue(key, out var value))
			{
				value.Add(panelCard);
				return;
			}
			tabPanelsList[key] = new List<Component> { panelCard };
		}
	}

	private ComponentBoxData GenerateComponentBoxData(string componentPanelSize, GridBox gridBoxSize, Point location)
	{
		PanelSize panelSizeObject = GetPanelSizeObject(componentPanelSize);
		if (panelSizeObject != null)
		{
			try
			{
				int w = (int)Math.Floor(panelSizeObject.Width / gridBoxSize.X);
				int h = (int)Math.Floor(panelSizeObject.Height / gridBoxSize.Y);
				int num = (int)Math.Ceiling((decimal)location.X / gridBoxSize.X);
				int num2 = (int)Math.Ceiling((decimal)location.Y / gridBoxSize.Y);
				return new ComponentBoxData
				{
					X = ((num == 0) ? 1 : num),
					Y = ((num2 == 0) ? 1 : num2),
					H = h,
					W = w
				};
			}
			catch (DivideByZeroException)
			{
				return new ComponentBoxData();
			}
		}
		return new ComponentBoxData();
	}

	public PanelSize GetPanelSizeObject(string str)
	{
		if (str == null)
		{
			return null;
		}
		str = str.Replace("X", "\"X\"");
		str = str.Replace("Y", "\"Y\"");
		str = str.Replace("Width", "\"Width\"");
		str = str.Replace("Height", "\"Height\"");
		str = str.Replace("=", ":");
		try
		{
			return JsonConvert.DeserializeObject<PanelSize>(str);
		}
		catch
		{
			return null;
		}
	}
}
