using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.EpControls;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Epicor.MetaFX.Core.Models.MetaUI.Patch;
using Epicor.MetaFX.Core.Models.MetaUI.RowRules;
using Epicor.MetaFX.Core.Models.MetaUI.Strings;
using Epicor.MetaFX.Core.Models.MetaUI.Tools;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Epicor.MetaFX.Core.TemplateManager;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Dashboard;

public class DashboardManager
{
	private readonly ILogger logger;

	private readonly IUserDefinedApplicationService userDefinedAppService;

	private readonly IDataDiscoveryService dataDiscoveryService;

	private readonly string rootPath;

	private readonly HelperMethods helperMethods;

	private readonly Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade schemaUpgrade;

	private readonly DashboardHelper dashboardHelper;

	private readonly LayoutHelper layoutHelper;

	private readonly Epicor.MetaFX.Core.TemplateManager.TemplateManager templateManager;

	private const string IMPORT_PATH = "../shared/imports/UIDashboards/base";

	private const int EPBOXGRID_COLUMNS = 24;

	private const int EPBOXGRID_ROWS = 12;

	private List<Group> groups = new List<Group>();

	private List<GroupInfo> groupInfo = new List<GroupInfo>();

	public DashboardManager(string rootPath, ILogger logger, IContentLoader contentLoader, IUserDefinedApplicationService userDefinedApplicationService, IDataDiscoveryService dataDiscoveryService)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		userDefinedAppService = userDefinedApplicationService;
		this.dataDiscoveryService = dataDiscoveryService;
		helperMethods = new HelperMethods(logger, contentLoader);
		schemaUpgrade = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(logger);
		layoutHelper = new LayoutHelper(logger);
		dashboardHelper = new DashboardHelper();
		templateManager = new Epicor.MetaFX.Core.TemplateManager.TemplateManager(rootPath, logger, contentLoader);
	}

	public EpMetaFxResponse<ReportResponse> Generate(DashboardRequest dashboardRequest, Func<string> currentCultureName, [Optional] Func<string, string, string> getMenuDescription)
	{
		EpMetaFxResponse<ReportResponse> epMetaFxResponse = new EpMetaFxResponse<ReportResponse>();
		if (dashboardRequest.DashboardDefinition?.DashBdDef == null || dashboardRequest.DashboardDefinition.DashBdDef.Count == 0)
		{
			logger.Debug("Invalid Dashboard definition.");
			epMetaFxResponse.Message = "Invalid Dashboard definition.";
			return epMetaFxResponse;
		}
		string text = "Ice.UIDbd." + dashboardRequest.DashboardDefinition.DashBdDef[0].DefinitionID;
		epMetaFxResponse.Data = new ReportResponse
		{
			Id = dashboardRequest.DashboardDefinition.DashBdDef[0].DefinitionID
		};
		string text2 = rootPath + "/" + text;
		helperMethods.EmptyDirectory(text2);
		try
		{
			object commonTemplate = templateManager.GetCommonTemplate(new EpMetaFxTemplateRequest
			{
				ViewType = "Dashboard"
			}, currentCultureName);
			List<MessageDetail> collection = GenerateInternal(text2, dashboardRequest.DashboardDefinition, dashboardRequest.DashboardDataSet, dashboardRequest.DynamicQueryDefinitions, commonTemplate);
			GenerateTools(text2, dashboardRequest.DashboardDataSet, getMenuDescription);
			GenerateStrings(text2);
			epMetaFxResponse.Details.AddRange(collection);
			epMetaFxResponse.IsSuccess = true;
			epMetaFxResponse.Data.FileName = text;
			epMetaFxResponse.Message = "Dashboard generated successfully.";
			userDefinedAppService.CreateNewRow(text);
			logger.Debug("Dashboard " + dashboardRequest.DashboardDefinition.DashBdDef[0].DefinitionID + " generated successfully.");
		}
		finally
		{
			if (!epMetaFxResponse.IsSuccess && Directory.Exists(text2))
			{
				Directory.Delete(text2, recursive: true);
			}
		}
		return epMetaFxResponse;
	}

	private List<MessageDetail> GenerateInternal(string outputPath, DashboardDefinition dashboardDefinition, DashboardDataSet dashboardDataSet, List<DynamicQueryDefinition> dynamicQueryDefinitions, object layoutTemplate)
	{
		int num = 1;
		List<MessageDetail> messages = new List<MessageDetail>();
		Dictionary<string, List<RowColumnChangedArgs>> dictionary = new Dictionary<string, List<RowColumnChangedArgs>>();
		DashboardArgs dashboardArgs = CreateDashboardArgs(dashboardDefinition, dashboardDataSet, outputPath, layoutTemplate);
		RowRulesManager rowRulesManager = new RowRulesManager(dashboardArgs);
		DataviewManager dataviewManager = new DataviewManager(dashboardArgs);
		PatchManager patchManager = new PatchManager(dashboardArgs);
		EventManager eventManager = new EventManager(dashboardArgs, delegate(MessageDetail messageDetail)
		{
			messages.Add(messageDetail);
		});
		LayoutManager layoutManager = new LayoutManager(dashboardArgs, delegate(MessageDetail messageDetail)
		{
			messages.Add(messageDetail);
		});
		DataViewSchema dataViewSchema = dataviewManager.GenerateSchema();
		RulesSchema rulesSchema = rowRulesManager.GenerateSchema();
		IEnumerable<bool> source = from query in dashboardDataSet.DashboardQuery
			from view in dashboardDataSet.DashboardView
			where view.QueryID.IsEqual(query.QueryID)
			from column in dashboardDataSet.DashboardViewColumns
			where column.ViewID == view.ViewID && column.QueryID == query.QueryID && column.DashboardPrompt
			select true;
		JToken jToken = layoutManager.GenerateSchema(layoutTemplate);
		List<Component> list = jToken["layout"]["components"].ToObject<List<Component>>();
		EventSchema eventSchema = eventManager.GenerateSchema();
		PatchSchema patchSchema = patchManager.GenerateSchema();
		List<DashboardViewArgs> list2 = new List<DashboardViewArgs>();
		List<DashboardViewArgs> list3 = new List<DashboardViewArgs>();
		List<PanelCardGridArgs> list4 = new List<PanelCardGridArgs>();
		new List<MetaFxPanelCard>();
		CartesianPlane cartesianPlane = default(CartesianPlane);
		string dataDiscoveryURL = GetDataDiscoveryURL(dashboardDataSet);
		if (string.IsNullOrEmpty(dataDiscoveryURL) && dashboardDataSet.DashboardView.Any((DashboardView x) => x.ViewType == "Chart" || x.ViewType == "Gauge"))
		{
			messages.Add(new MessageDetail
			{
				Message = "Please install Data Discovery to convert charts/gauge",
				Type = "warning"
			});
		}
		DashboardQuery[] dashboardQuery = dashboardDataSet.DashboardQuery;
		foreach (DashboardQuery dashboardQuery2 in dashboardQuery)
		{
			List<DashboardView> list5 = dashboardDataSet.DashboardView.Where((DashboardView dbd) => dbd.QueryID.IsEqual(dashboardQuery2.QueryID)).ToList();
			list5.Where((DashboardView dv) => dv.ViewType.IsEqual("Grid")).ToList();
			foreach (DashboardViewArgs dashboardViewArgs in dashboardHelper.CreateDashboardViewArgsList(dashboardDataSet, dashboardQuery2, list5, source.Any()))
			{
				if (!dataViewSchema.DataViews.Any((DataView x) => x.Id == dashboardViewArgs.DataViewName))
				{
					dataViewSchema.DataViews.Add(dataviewManager.GenerateDataView(dashboardViewArgs));
					if (dashboardViewArgs.IsTrackerEmbedGrid)
					{
						dataViewSchema.DataViews.Add(dataviewManager.GenerateDataViewForEmbedGrid(dashboardViewArgs));
					}
				}
			}
		}
		(CartesianPlane, decimal, decimal) tuple = DefineLayoutCartesianPlane(dashboardDataSet.DashboardView, layoutManager);
		cartesianPlane = tuple.Item1;
		layoutManager.gridBoxSize = GetGridBoxSize(cartesianPlane);
		dashboardQuery = dashboardDataSet.DashboardQuery;
		foreach (DashboardQuery dashboardQuery3 in dashboardQuery)
		{
			DynamicQueryDefinition dynamicQueryDefinition = dynamicQueryDefinitions.FirstOrDefault((DynamicQueryDefinition dqf) => dqf.DynamicQuery?.Any((DynamicQuery p) => p.QueryID.IsEqual(dashboardQuery3.DynamicQueryID)) ?? false);
			if (dynamicQueryDefinition == null)
			{
				messages.Add(new MessageDetail
				{
					Type = "warning",
					Message = "DynamicQueryDefinition " + dashboardQuery3.DynamicQueryID + " does not exists."
				});
				continue;
			}
			PanelCardGridArgs panelCardGridArgs = new PanelCardGridArgs();
			panelCardGridArgs.DashboardQuery = dashboardQuery3;
			panelCardGridArgs.DynamicQueryDefinition = dynamicQueryDefinition;
			List<DashboardView> list6 = dashboardDataSet.DashboardView.Where((DashboardView dbd) => dbd.QueryID.IsEqual(dashboardQuery3.QueryID)).ToList();
			List<DashboardView> gridViews = list6.Where((DashboardView dv) => dv.ViewType.IsEqual("Grid")).ToList();
			List<DashboardViewArgs> list7 = dashboardHelper.CreateDashboardViewArgsList(dashboardDataSet, dashboardQuery3, list6, source.Any());
			List<Component> list8 = new List<Component>();
			List<Component> list9 = new List<Component>();
			List<DashboardViewArgs> list10 = new List<DashboardViewArgs>();
			int num3 = 0;
			foreach (DashboardViewArgs item3 in list7)
			{
				item3.DashboardView.ScreenLocation = UpdateQueryLocation(item3.DashboardView.ScreenLocation, (int)tuple.Item2, (int)tuple.Item3);
				item3.DynamicQueryDefinition = dynamicQueryDefinition;
				if ((item3.IsChart || item3.IsGauge) && !string.IsNullOrEmpty(dataDiscoveryURL))
				{
					if (item3.IsGauge)
					{
						DashboardGauge dashboardGauge = item3.DashboardView.DashboardGauge;
						item3.DashboardView.DasboardChart = new DashboardChart
						{
							BAQ = dashboardGauge.BAQ,
							ChartCaption = dashboardGauge.GaugeCaption,
							DashboardDefinitionID = dashboardGauge.DashboardDefinitionID,
							Filters = dashboardGauge.Filters,
							Id = dashboardGauge.Id,
							ChartType = dashboardGauge.GaugeType
						};
						if (int.TryParse(item3.DashboardView.DashboardGauge.gaugeDataSet.GaugeTable.MarkerBinding, out var _))
						{
							messages.Add(new MessageDetail
							{
								Type = "warning",
								Message = "Marker binding of type number for dashboard '" + item3.DashboardView.DashboardGauge.GaugeCaption + "' is not supported. And will not be converted."
							});
							continue;
						}
					}
					GenerateDashboardChart(messages, layoutManager, list9, item3, dataDiscoveryURL, dashboardDataSet, num3++);
				}
				if (item3.IsSearchTracker)
				{
					list2.Add(item3);
				}
				if (item3.IsTrackerEmbedGrid)
				{
					list10.Add(item3);
				}
				if (dashboardDataSet.DashboardViewRules != null)
				{
					rulesSchema.RowRules.AddRange(rowRulesManager.GenerateRules(item3));
				}
				if (item3.IsSearchTrackerSlider && !item3.IsTracker)
				{
					num++;
					layoutManager.CreateSearchTrackerPage(item3, num.ToString(), out var trackerComponents);
					list3.Add(item3);
					list.AddRange(trackerComponents);
				}
				else if (item3.IsTracker)
				{
					num++;
					List<Component> collection = layoutManager.GenerateTrackerPanel(item3, num.ToString());
					if (!panelCardGridArgs.DashboardViewArgsList.Any())
					{
						list.AddRange(collection);
					}
					else
					{
						list8.AddRange(collection);
					}
					if (item3.IsTrackerEmbedGrid)
					{
						item3.ClientFilter = eventManager.GetFilters(item3, dictionary, panelCardGridArgs);
					}
				}
				else if (item3.IsGrid)
				{
					item3.ClientFilter = eventManager.GetFilters(item3, dictionary, panelCardGridArgs);
					panelCardGridArgs.DashboardViewArgsList.Add(item3);
					if (!list4.Contains(panelCardGridArgs))
					{
						list4.Add(panelCardGridArgs);
					}
				}
				if (item3.IsSearchTracker)
				{
					List<BAQWhereExpression> searchFilter = eventManager.GetSearchFilter(item3, panelCardGridArgs);
					if (searchFilter != null)
					{
						panelCardGridArgs.SearchFilters.Add(searchFilter);
					}
				}
				eventSchema.Events.AddRange(eventManager.GenerateEvents(item3, gridViews, dictionary));
				groupInfo.Add(dashboardHelper.GetGroupsColumns(item3, dashboardArgs));
			}
			if (panelCardGridArgs.DashboardViewArgsList.Any())
			{
				panelCardGridArgs.DashboardViewColumns = dashboardHelper.GetDashboardViewColumns(panelCardGridArgs.DashboardViewArgsList);
				panelCardGridArgs.DashboardViewImageColumns = dashboardHelper.GetDashboardViewImageColumns(panelCardGridArgs.DashboardViewArgsList);
				panelCardGridArgs.ServerFilter = eventManager.GetWhereClauses(panelCardGridArgs, dictionary);
				foreach (PanelCardGridArgs panelCard in dashboardHelper.SplitPanelCardGrids(panelCardGridArgs))
				{
					if (panelCard.DashboardViewArgsList.Any())
					{
						Component item = layoutManager.GenerateGridPanel(group: (groupInfo?.FirstOrDefault((GroupInfo x) => x.QueryID == panelCard.DashboardQuery.QueryID && x.ViewID == panelCard.DashboardViewColumns[0].ViewID))?.Groups, panelCardGridArgs: panelCard);
						list.Add(item);
						eventSchema.Events.AddRange(eventManager.GenerateEvents(panelCard, dashboardDataSet, dataViewSchema, dashboardQuery3));
					}
				}
			}
			foreach (Component item4 in list8)
			{
				list.Add(item4);
			}
			foreach (Component item5 in list9)
			{
				list.Add(item5);
			}
			if (!list10.Any())
			{
				continue;
			}
			foreach (DashboardViewArgs embedGridDataViewArgs in list10)
			{
				PanelCardGridArgs panelCardGridArgs2 = dashboardHelper.Create(panelCardGridArgs, new List<DashboardViewArgs> { embedGridDataViewArgs });
				panelCardGridArgs2.ServerFilter = eventManager.GetWhereClauses(panelCardGridArgs2, dictionary);
				Component component = list.FirstOrDefault((Component c) => c.Model.Id == (dashboardHelper.GetPanelName(embedGridDataViewArgs) ?? "") && c.Model.Class == "PanelCard");
				Component item2 = layoutManager.GenerateGridPanel(panelCardGridArgs2, component.Id);
				list4.Add(panelCardGridArgs2);
				if (component != null)
				{
					((MetaFxPanelCard)component.Model).BodyComponents.Add(item2);
				}
				string dataviewNameForEmbedGrid = dashboardHelper.GetDataviewNameForEmbedGrid(embedGridDataViewArgs);
				dictionary.Add(dataviewNameForEmbedGrid, new List<RowColumnChangedArgs>());
				dictionary[dataviewNameForEmbedGrid].Add(new RowColumnChangedArgs
				{
					Type = RowColumnChangedType.copyRow,
					FromDataView = dataviewNameForEmbedGrid,
					ToDataView = embedGridDataViewArgs.DataViewName
				});
				eventSchema.Events.AddRange(eventManager.GenerateEmbedGridEvents(panelCardGridArgs2));
			}
		}
		eventSchema.Events.AddRange(eventManager.GenerateProcessLinkEvents(Path.GetFileName(outputPath), dashboardDataSet));
		for (int num4 = 0; num4 < dashboardDataSet.DashboardURLs?.Length; num4++)
		{
			DashboardURL dashboardURL = dashboardDataSet.DashboardURLs[num4];
			if (!dashboardURL.URLAddress.StartsWith("https", StringComparison.InvariantCultureIgnoreCase))
			{
				messages.Add(new MessageDetail
				{
					Type = "warning",
					Message = "Widget url for " + dashboardURL.Caption + " is not secure."
				});
			}
			foreach (QueryPhraseSubscribers subscriber in dashboardHelper.GetSubscriberList(dashboardURL.QueryPhraseSubscribers))
			{
				DashboardQueryPublishers publisher = dashboardDataSet.DashboardQueryPublishers.FirstOrDefault((DashboardQueryPublishers x) => x.PublisherID.Equals(subscriber.PublisherID));
				DashboardQuery attachedQuery = dashboardDataSet.DashboardQuery.FirstOrDefault((DashboardQuery x) => x.QueryID.Equals(publisher.QueryID));
				DataView dataView = dataViewSchema.DataViews.FirstOrDefault((DataView x) => x.Data.Equals(attachedQuery.DynamicQueryID));
				dashboardHelper.InterpolateUrlParams(dataView?.Id, publisher?.PublishedColumn, subscriber.Token, dashboardURL);
			}
			list.AddRange(layoutManager.GenerateWebViewLayout(dashboardURL));
		}
		eventSchema.Events.AddRange(eventManager.GenerateInitialEvents(list2, list4, list3, dictionary));
		foreach (KeyValuePair<string, List<RowColumnChangedArgs>> item6 in dictionary)
		{
			eventSchema.Events.Add(eventManager.CreateRowChangedEvent(item6.Key, item6.Value));
		}
		GeneratePanelCardStackComponents(layoutManager, list);
		AdjustComponentsPosition(list);
		dataviewManager.CreateFile(dataViewSchema);
		rowRulesManager.CreateFile(rulesSchema);
		patchManager.CreateFile(patchSchema);
		jToken["layout"]["components"] = layoutHelper.ConvertToJArray(list);
		if (list.Where((Component comp) => comp.ParentId == null).Count() > 16)
		{
			jToken["layout"]["fitToPageMode"] = false;
			messages.Add(new MessageDetail
			{
				Type = "warning",
				Message = "Dashboard is converted in scrollable layout because the number of cards exceeds the maximum limit."
			});
		}
		FlattenLayout flattenLayout = new FlattenLayout();
		jToken["layout"] = flattenLayout.ConvertToFlattenLayout(jToken["layout"] as JObject);
		layoutManager.CreateFile(jToken);
		eventManager.CreateFile(eventSchema);
		return messages;
	}

	private DashboardArgs CreateDashboardArgs(DashboardDefinition dashboardDefinition, DashboardDataSet dashboardDataSet, string outputPath, object layoutTemplate)
	{
		DashboardArgs obj = new DashboardArgs
		{
			HelperMethods = helperMethods,
			DashboardHelper = dashboardHelper,
			LayoutHelper = layoutHelper,
			SchemaUpgrade = schemaUpgrade,
			TemplateManager = templateManager,
			Logger = logger,
			DashboardDataSet = dashboardDataSet,
			DashboardDefinition = dashboardDefinition
		};
		DashboardQuery[] dashboardQuery = dashboardDataSet.DashboardQuery;
		int isOnlyOnePanel;
		if (dashboardQuery != null && dashboardQuery.Length == 1)
		{
			DashboardView[] dashboardView = dashboardDataSet.DashboardView;
			isOnlyOnePanel = ((dashboardView != null && dashboardView.Length == 1) ? 1 : 0);
		}
		else
		{
			isOnlyOnePanel = 0;
		}
		obj.IsOnlyOnePanel = (byte)isOnlyOnePanel != 0;
		obj.ImportPath = "../shared/imports/UIDashboards/base";
		obj.OutputPath = outputPath;
		obj.LayoutTemplate = layoutTemplate;
		obj.RootPath = rootPath;
		return obj;
	}

	private void GenerateTools(string path, DashboardDataSet dashboardDataSet, [Optional] Func<string, string, string> getMenuDescription)
	{
		List<object> list = new List<object>();
		ToolsSchema toolsSchema = new ToolsSchema();
		toolsSchema.Schema = "https://schemas.developer.epicor.com/schemas/v2.0/tools.json";
		toolsSchema.ContentVersion = "2.0";
		toolsSchema.Imports = new object[1]
		{
			new
			{
				tools = new Reference
				{
					Refs = "../shared/imports/UIDashboards/base/tools.jsonc#/tools"
				}
			}
		};
		ToolsSchema toolsSchema2 = toolsSchema;
		if (dashboardDataSet.DashboardProcess != null && getMenuDescription != null)
		{
			DashboardProcess[] dashboardProcess = dashboardDataSet.DashboardProcess;
			foreach (DashboardProcess dashboardProcess2 in dashboardProcess)
			{
				string text = getMenuDescription(dashboardProcess2.Company, dashboardProcess2.MenuItemID);
				list.Add(new
				{
					id = "ToolRedirectTo" + dashboardProcess2.MenuItemID,
					parentId = "epView",
					icon = "mdi mdi-launch",
					text = text,
					epBinding = "TransView.ToolRedirectTo" + dashboardProcess2.MenuItemID
				});
			}
		}
		toolsSchema2.Tools = list.ToArray();
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(toolsSchema2), "tools");
		helperMethods.CreateJsonFile(data, path, "tools.jsonc");
	}

	private void GenerateStrings(string path)
	{
		StringsSchema stringsSchema = new StringsSchema();
		stringsSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1/strings.json";
		stringsSchema.ContentVersion = "1.0.0";
		stringsSchema.Imports = new object[1]
		{
			new
			{
				@default = new Reference
				{
					Refs = "../shared/imports/UIDashboards/base/strings.jsonc#/strings"
				}
			}
		};
		StringsSchema o = stringsSchema;
		JObject data = schemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(o), "strings");
		helperMethods.CreateJsonFile(data, path, "strings.jsonc");
	}

	private string GetDataDiscoveryURL(DashboardDataSet dashboardDataSet)
	{
		if (dashboardDataSet.DashboardView.Any((DashboardView x) => x.ViewType == "Chart" || x.ViewType == "Gauge"))
		{
			return dataDiscoveryService.GetDataDiscoveryURL();
		}
		return string.Empty;
	}

	private void GenerateDashboardChart(List<MessageDetail> messages, LayoutManager dashboardLayoutHelper, List<Component> chartComponents, DashboardViewArgs dashboardViewArgs, string dataDiscoveryURL, DashboardDataSet dashboardDataSet, int gaugeChartCount)
	{
		EpAppWidget widgets = dataDiscoveryService.GetWidgets();
		if (widgets == null)
		{
			return;
		}
		List<MessageDetail> list = new List<MessageDetail>();
		if (dashboardViewArgs.DashboardView.DasboardChart.Filters != null && dashboardViewArgs.DashboardView.DasboardChart.Filters.Count() > 0)
		{
			list.Add(new MessageDetail
			{
				Message = "Chart: " + (dashboardViewArgs.IsGauge ? dashboardViewArgs.DashboardView.DashboardGauge.GaugeCaption : dashboardViewArgs.DashboardView.DasboardChart.ChartCaption),
				Type = "warning"
			});
			IEnumerable<DashboardChartFilter> enumerable = dashboardViewArgs.DashboardView.DasboardChart.Filters.Where((DashboardChartFilter x) => (x.IsValueAnotherColumn || x.Value.StartsWith("Constant:")) && x.Condition != "BEGINS");
			if (enumerable.Count() > 0)
			{
				dashboardViewArgs.DashboardView.DasboardChart.Filters = dashboardViewArgs.DashboardView.DasboardChart.Filters.Except(enumerable).ToList();
				dataDiscoveryService.SetPubSubModel(widgets, enumerable.ToList());
			}
		}
		if (dataDiscoveryService.SaveDashboardChart(dataDiscoveryURL, dashboardViewArgs.DashboardView) != null)
		{
			if (list.Count() > 1)
			{
				messages.AddRange(list);
			}
			chartComponents.AddRange(dashboardLayoutHelper.GenerateChartPanel(dashboardViewArgs, widgets, gaugeChartCount));
		}
	}

	private (CartesianPlane cartesianPlane, decimal xOffset, decimal yOffset) DefineLayoutCartesianPlane(DashboardView[] dashboardViews, LayoutManager dashboardLayoutHelper)
	{
		CartesianPlane cartesianPlane = default(CartesianPlane);
		foreach (DashboardView dashboardView in dashboardViews)
		{
			PanelSize panelSizeObject = dashboardLayoutHelper.GetPanelSizeObject(dashboardView.PanelSize);
			if (panelSizeObject != null && (!dashboardView.TabId.HasValue || (dashboardView.TabId.HasValue && dashboardView.IsSelectedTab == true)))
			{
				cartesianPlane = UpdateClassicLayoutCartesianPlane(cartesianPlane, panelSizeObject, dashboardView.ScreenLocation);
			}
		}
		return SetCartesianPlaneCenter(cartesianPlane);
	}

	private CartesianPlane UpdateClassicLayoutCartesianPlane(CartesianPlane plane, PanelSize componentPanelSize, Point location)
	{
		if ((decimal)location.X < plane.Xmin || plane.Xmin == 0m)
		{
			plane.Xmin = location.X;
		}
		if ((decimal)location.X + componentPanelSize.Width > plane.Xmax)
		{
			plane.Xmax = (decimal)location.X + componentPanelSize.Width;
		}
		if ((decimal)location.Y < plane.Ymin || plane.Ymin == 0m)
		{
			plane.Ymin = location.Y;
		}
		if ((decimal)location.Y + componentPanelSize.Height > plane.Ymax)
		{
			plane.Ymax = (decimal)location.Y + componentPanelSize.Height;
		}
		return plane;
	}

	private (CartesianPlane cartesianPlane, decimal xOffset, decimal yOffset) SetCartesianPlaneCenter(CartesianPlane cartesianPlane)
	{
		decimal num = 0m;
		decimal num2 = 0m;
		if (cartesianPlane.Xmin > 0m)
		{
			num = cartesianPlane.Xmin;
			cartesianPlane.Xmin = 0m;
			cartesianPlane.Xmax -= num;
		}
		if (cartesianPlane.Ymin > 0m)
		{
			num2 = cartesianPlane.Ymin;
			cartesianPlane.Ymin = 0m;
			cartesianPlane.Ymax -= num2;
		}
		return (cartesianPlane: cartesianPlane, xOffset: num, yOffset: num2);
	}

	private Point UpdateQueryLocation(Point queryLocation, int xOffset, int yOffset)
	{
		Point result = new Point(queryLocation.X, queryLocation.Y);
		result.Offset(xOffset * -1, yOffset * -1);
		return result;
	}

	private GridBox GetGridBoxSize(CartesianPlane plane)
	{
		return new GridBox
		{
			X = (plane.Xmax - plane.Xmin) / 24m,
			Y = (plane.Ymax - plane.Ymin) / 12m
		};
	}

	private void GeneratePanelCardStackComponents(LayoutManager layoutManager, List<Component> components)
	{
		foreach (List<Component> value in layoutManager.tabPanelsList.Values)
		{
			if (value.Count <= 1)
			{
				continue;
			}
			Component component = layoutManager.GeneratePanelCardStack(value);
			List<int> list = new List<int>();
			foreach (Component item in value)
			{
				((MetaFxPanelCard)item.Model).Stacked = true;
				if (item.IsSelectedTab == true)
				{
					component.BoxData = item.BoxData;
					item.BoxData = null;
				}
				if (item.TabIndex.HasValue)
				{
					list.Add(item.TabIndex.GetValueOrDefault());
				}
				item.IsSelectedTab = null;
				item.TabIndex = null;
			}
			PanelCardStackItem[] array = ((MetaFxPanelCardStack)component.Model).Panels.ToArray();
			Array.Sort(list.ToArray(), array);
			((MetaFxPanelCardStack)component.Model).Panels = array.ToList();
			components.Add(component);
		}
	}

	private void AdjustComponentsPosition(List<Component> components)
	{
		Dictionary<int, ComponentBoxData> dictionary = new Dictionary<int, ComponentBoxData>();
		Dictionary<int, ComponentBoxData> dictionary2 = new Dictionary<int, ComponentBoxData>();
		bool flag = VerifyAndAdjustGridDimension(components, LayoutDimension.Rows, dictionary);
		if (!VerifyAndAdjustGridDimension(components, LayoutDimension.Columns, dictionary2) && !flag)
		{
			UpdateComponentsPosition(components, dictionary, dictionary2);
		}
		else
		{
			UpdateComponentsPosition(components);
		}
	}

	private bool VerifyAndAdjustGridDimension(List<Component> components, LayoutDimension dimension, Dictionary<int, ComponentBoxData> modifiedBoxDataComponents)
	{
		int num = ((dimension == LayoutDimension.Rows) ? 12 : 24);
		new List<int>();
		for (int i = 1; i <= num; i++)
		{
			Dictionary<int, ComponentBoxData> componentsInDimension = GetComponentsInDimension(components, dimension, i, modifiedBoxDataComponents);
			List<KeyValuePair<int, ComponentBoxData>> list = SortBoxDataComponentsByDimension(componentsInDimension, dimension);
			if (componentsInDimension.Keys.Count > 4)
			{
				return true;
			}
			if (componentsInDimension.Keys.Count == 4)
			{
				List<int> list2 = ((dimension == LayoutDimension.Rows) ? new List<int> { 1, 7, 13, 19 } : new List<int> { 1, 4, 7, 10 });
				for (int j = 0; j < list.Count; j++)
				{
					KeyValuePair<int, ComponentBoxData> keyValuePair = list[j];
					switch (dimension)
					{
					case LayoutDimension.Rows:
						keyValuePair.Value.W = 6;
						keyValuePair.Value.X = list2[j];
						keyValuePair.Value.MaxSharedComponentsInRow = 4;
						break;
					case LayoutDimension.Columns:
						keyValuePair.Value.H = 3;
						keyValuePair.Value.Y = list2[j];
						keyValuePair.Value.MaxSharedComponentsInColumn = 4;
						break;
					}
					modifiedBoxDataComponents[keyValuePair.Key] = keyValuePair.Value;
				}
			}
			else if (componentsInDimension.Keys.Count == 3)
			{
				List<int> list3 = ((dimension == LayoutDimension.Rows) ? new List<int> { 1, 9, 17 } : new List<int> { 1, 5, 9 });
				for (int k = 0; k < list.Count; k++)
				{
					KeyValuePair<int, ComponentBoxData> keyValuePair2 = list[k];
					if (dimension == LayoutDimension.Rows && (!keyValuePair2.Value.MaxSharedComponentsInRow.HasValue || keyValuePair2.Value.MaxSharedComponentsInRow <= 3))
					{
						keyValuePair2.Value.W = 8;
						keyValuePair2.Value.X = list3[k];
						keyValuePair2.Value.MaxSharedComponentsInRow = 3;
					}
					else if (dimension == LayoutDimension.Columns && (!keyValuePair2.Value.MaxSharedComponentsInColumn.HasValue || keyValuePair2.Value.MaxSharedComponentsInColumn <= 3))
					{
						keyValuePair2.Value.H = 4;
						keyValuePair2.Value.Y = list3[k];
						keyValuePair2.Value.MaxSharedComponentsInColumn = 3;
					}
					modifiedBoxDataComponents[keyValuePair2.Key] = keyValuePair2.Value;
				}
			}
			else if (componentsInDimension.Keys.Count == 2)
			{
				for (int l = 0; l < 2; l++)
				{
					KeyValuePair<int, ComponentBoxData> keyValuePair3 = list[l];
					if (dimension == LayoutDimension.Rows && (!keyValuePair3.Value.MaxSharedComponentsInRow.HasValue || keyValuePair3.Value.MaxSharedComponentsInRow <= 2))
					{
						keyValuePair3.Value.X = ((l == 0) ? 1 : (list[0].Value.W + 1));
						keyValuePair3.Value.W = ((l != 0) ? (24 - list[0].Value.W) : ((keyValuePair3.Value.W < 6) ? 6 : keyValuePair3.Value.W));
						if (l == 1 && keyValuePair3.Value.W < ComponentBoxData.MINIMUM_COLUMN_SIZE)
						{
							keyValuePair3.Value.W = ComponentBoxData.MINIMUM_COLUMN_SIZE;
							keyValuePair3.Value.X = 24 - ComponentBoxData.MINIMUM_COLUMN_SIZE + 1;
							list[0].Value.W = 24 - ComponentBoxData.MINIMUM_COLUMN_SIZE;
						}
						keyValuePair3.Value.MaxSharedComponentsInRow = 2;
					}
					else if (dimension == LayoutDimension.Columns && (!keyValuePair3.Value.MaxSharedComponentsInColumn.HasValue || keyValuePair3.Value.MaxSharedComponentsInColumn <= 2))
					{
						keyValuePair3.Value.Y = ((l == 0) ? 1 : (list[0].Value.H + 1));
						keyValuePair3.Value.H = ((l != 0) ? (12 - list[0].Value.H) : ((keyValuePair3.Value.H < 3) ? 3 : keyValuePair3.Value.H));
						if (l == 1 && keyValuePair3.Value.H < ComponentBoxData.MINIMUM_ROW_SIZE)
						{
							keyValuePair3.Value.H = ComponentBoxData.MINIMUM_ROW_SIZE;
							keyValuePair3.Value.Y = 12 - ComponentBoxData.MINIMUM_ROW_SIZE + 1;
							list[0].Value.H = 12 - ComponentBoxData.MINIMUM_ROW_SIZE;
						}
						keyValuePair3.Value.MaxSharedComponentsInColumn = 2;
					}
					modifiedBoxDataComponents[keyValuePair3.Key] = keyValuePair3.Value;
				}
			}
			else if (componentsInDimension.Keys.Count == 1)
			{
				KeyValuePair<int, ComponentBoxData> keyValuePair4 = list[0];
				if (dimension == LayoutDimension.Rows && (!keyValuePair4.Value.MaxSharedComponentsInRow.HasValue || keyValuePair4.Value.MaxSharedComponentsInRow <= 1))
				{
					keyValuePair4.Value.W = 24;
					keyValuePair4.Value.X = 1;
					keyValuePair4.Value.MaxSharedComponentsInRow = 1;
				}
				else if (dimension == LayoutDimension.Columns && (!keyValuePair4.Value.MaxSharedComponentsInColumn.HasValue || keyValuePair4.Value.MaxSharedComponentsInColumn <= 1))
				{
					keyValuePair4.Value.H = 12;
					keyValuePair4.Value.Y = 1;
					keyValuePair4.Value.MaxSharedComponentsInColumn = 1;
				}
				modifiedBoxDataComponents[keyValuePair4.Key] = keyValuePair4.Value;
			}
		}
		return false;
	}

	private Dictionary<int, ComponentBoxData> GetComponentsInDimension(List<Component> components, LayoutDimension dimension, int dimensionIndex, Dictionary<int, ComponentBoxData> modifiedBoxDataComponents)
	{
		Dictionary<int, ComponentBoxData> dictionary = new Dictionary<int, ComponentBoxData>();
		for (int i = 0; i < components.Count; i++)
		{
			Component component = components[i];
			if (component.BoxData != null)
			{
				bool flag = false;
				switch (dimension)
				{
				case LayoutDimension.Rows:
				{
					int y = component.BoxData.Y;
					int num2 = component.BoxData.Y + component.BoxData.H - 1;
					flag = dimensionIndex >= y && dimensionIndex <= num2;
					break;
				}
				case LayoutDimension.Columns:
				{
					int x = component.BoxData.X;
					int num = component.BoxData.X + component.BoxData.W - 1;
					flag = dimensionIndex >= x && dimensionIndex <= num;
					break;
				}
				}
				if (flag)
				{
					ComponentBoxData componentBoxData = new ComponentBoxData
					{
						X = component.BoxData.X,
						Y = component.BoxData.Y,
						H = component.BoxData.H,
						W = component.BoxData.W
					};
					dictionary[i] = (modifiedBoxDataComponents.ContainsKey(i) ? modifiedBoxDataComponents[i] : componentBoxData);
				}
			}
		}
		return dictionary;
	}

	private void UpdateComponentsPosition(List<Component> components, Dictionary<int, ComponentBoxData> horizontalPositions = null, Dictionary<int, ComponentBoxData> verticalPositions = null)
	{
		if (horizontalPositions == null && verticalPositions == null)
		{
			foreach (Component component in components)
			{
				if (component.BoxData != null)
				{
					component.BoxData.W = ComponentBoxData.MINIMUM_COLUMN_SIZE;
					component.BoxData.H = ComponentBoxData.MINIMUM_ROW_SIZE;
					component.BoxData.X = 1;
					component.BoxData.Y = 1;
				}
			}
			return;
		}
		foreach (int key in horizontalPositions.Keys)
		{
			components[key].BoxData.X = horizontalPositions[key].X;
			components[key].BoxData.W = horizontalPositions[key].W;
		}
		foreach (int key2 in verticalPositions.Keys)
		{
			components[key2].BoxData.Y = verticalPositions[key2].Y;
			components[key2].BoxData.H = verticalPositions[key2].H;
		}
		foreach (Component component2 in components)
		{
			if (component2.BoxData != null)
			{
				component2.BoxData.W = ((component2.BoxData.W < ComponentBoxData.MINIMUM_COLUMN_SIZE) ? ComponentBoxData.MINIMUM_COLUMN_SIZE : component2.BoxData.W);
				component2.BoxData.H = ((component2.BoxData.H < ComponentBoxData.MINIMUM_ROW_SIZE) ? ComponentBoxData.MINIMUM_ROW_SIZE : component2.BoxData.H);
			}
		}
	}

	private List<KeyValuePair<int, ComponentBoxData>> SortBoxDataComponentsByDimension(Dictionary<int, ComponentBoxData> components, LayoutDimension dimension)
	{
		if (dimension == LayoutDimension.Rows)
		{
			return components.OrderBy(delegate(KeyValuePair<int, ComponentBoxData> entry)
			{
				KeyValuePair<int, ComponentBoxData> keyValuePair = entry;
				return keyValuePair.Value.X;
			}).ToList();
		}
		return components.OrderBy(delegate(KeyValuePair<int, ComponentBoxData> entry)
		{
			KeyValuePair<int, ComponentBoxData> keyValuePair = entry;
			return keyValuePair.Value.Y;
		}).ToList();
	}
}
