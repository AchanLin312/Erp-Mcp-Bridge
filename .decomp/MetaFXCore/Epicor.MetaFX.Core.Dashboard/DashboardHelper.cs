using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Dashboard;

internal class DashboardHelper
{
	public class CustomJsonWriter : JsonTextWriter
	{
		public CustomJsonWriter(TextWriter writer)
			: base(writer)
		{
		}

		public override void WritePropertyName(string name)
		{
			if (name.StartsWith("@") || name.StartsWith("#"))
			{
				base.WritePropertyName(name.Substring(1));
			}
			else
			{
				base.WritePropertyName(name);
			}
		}
	}

	public const string TRANSVIEW = "TransView";

	public const string CC_BPM_DATA = "CallContextBpmData";

	public const string CONSTANT = "Constant";

	public const string FIELDUPDATECOL = "FieldUpdateCol";

	public const string ACTIONID = "ActionId";

	public const string EDITOR_TEXT = "text";

	public const string EDITOR_NUMERIC = "numeric";

	public const string EDITOR_DATE = "date";

	public const string EDITOR_BOOLEAN = "boolean";

	public const string DATATYPE_NVARCHAR = "nvarchar";

	public const string DATATYPE_CHARACTER = "character";

	public const string DATATYPE_BIT = "bit";

	public const string DATATYPE_LOGICAL = "logical";

	public const string DATATYPE_DATETIME = "datetime";

	public const string DATATYPE_DATE = "date";

	public const string DATATYPE_DATETIMEOFFSET = "datetimeoffset";

	public const string DATATYPE_DECIMAL = "decimal";

	public const string DATATYPE_BIGINT = "bigint";

	public const string DATATYPE_INT = "int";

	public const string DATATYPE_INTEGER = "integer";

	public const string EMBEDGRID = "EmbedGrid";

	public const string ADVANCE_SEARCH = "Advance Search";

	private const string DateFormat = "yyyy-MM-dd";

	public const string DASHBOARD_CONDITION_EQUAL = "Equals";

	public const string DASHBOARD_CONDITION_NOTEQUAL = "NotEqual";

	public const string DASHBOARD_CONDITION_GREATERTHAN = "GreaterThan";

	public const string DASHBOARD_CONDITION_LESSTHAN = "LessThan";

	public const string DASHBOARD_CONDITION_GREATERTHANOREQUALTO = "GreaterThanOrEqualTo";

	public const string DASHBOARD_CONDITION_LESSTHANOREQUALTO = "LessThanOrEqualTo";

	public const string DASHBOARD_CONDITION_STARTSWITH = "StartsWith";

	public const string DASHBOARD_CONDITION_MATCHES = "Matches";

	private GroupInfo groupInfo = new GroupInfo();

	public bool HasSliderSearch(DashboardDataSet dashboardDataSet, DashboardQuery dashboardQuery, out DashboardView dashboardTracker)
	{
		dashboardTracker = null;
		foreach (DashboardView tracker in dashboardDataSet.DashboardView.Where((DashboardView v) => v.QueryID == dashboardQuery.QueryID && v.ViewType == "Tracker"))
		{
			if ((dashboardDataSet.DashboardViewOptions?.FirstOrDefault((DashboardViewOptions qo) => qo.PropertyName == "InputPrompts" && qo.QueryID == tracker.QueryID && qo.ViewID == tracker.ViewID))?.PropertyValue == "True")
			{
				dashboardTracker = tracker;
				return true;
			}
		}
		return false;
	}

	public string GetFirstSliderSearchViewEvent(List<DashboardViewArgs> sliderSearchPanels)
	{
		if (sliderSearchPanels.Any())
		{
			return GetLayoutSearchButtonName(sliderSearchPanels[0]);
		}
		return null;
	}

	public string GetDataViewName(DashboardDataSet dashboardDataSet, DashboardQuery dashboardQuery, DashboardView dashboardView, List<DashboardView> dashboardViews = null, bool isTrackerView = false)
	{
		int dashboardQueryIndex = Array.IndexOf(dashboardDataSet.DashboardQuery, dashboardQuery);
		DashboardView[] obj = dashboardViews?.Where((DashboardView dv) => dv.QueryID == dashboardQuery.QueryID).ToArray();
		int dashboardViewIndex = ((obj == null || obj.Count() <= 1 || isTrackerView) ? Array.IndexOf(dashboardDataSet.DashboardView.Where((DashboardView dv) => dv.QueryID == dashboardQuery.QueryID).ToArray(), dashboardView) : 0);
		return GetDataViewName(dashboardQuery, dashboardQueryIndex, dashboardViewIndex);
	}

	public string GetDataViewName(DashboardQuery dashboardQuery, int dashboardQueryIndex, int dashboardViewIndex)
	{
		string text = (Regex.IsMatch(dashboardQuery.DynamicQueryID, "^\\d") ? "v" : "");
		return string.Format("{0}{1}_{2}_{3}", text, dashboardQuery.DynamicQueryID.Replace("-", ""), dashboardQueryIndex, dashboardViewIndex);
	}

	public string GetSearchableTrackerPageName(DashboardDataSet dashboardDataSet, DashboardQuery dashboardQuery, DashboardView dashboardView)
	{
		return GetDataViewName(dashboardDataSet, dashboardQuery, dashboardView) ?? "";
	}

	public string GetRefreshEventName(PanelCardGridArgs panelCardGridArgs)
	{
		return "refresh_" + panelCardGridArgs.DashboardViewArgsList[0].DataViewName;
	}

	public string GetTransViewName(PanelCardGridArgs panelCardGridArgs, string column)
	{
		return panelCardGridArgs.DashboardViewArgsList[0].DataViewName + "_" + column;
	}

	public string GetPanelName(DashboardViewArgs dashboardViewArgs, string sequenceNumber = null)
	{
		if (dashboardViewArgs.IsTrackerEmbedGrid)
		{
			return "panel_" + dashboardViewArgs.DataViewName + "_EmbedGrid";
		}
		if (sequenceNumber != null)
		{
			return "panel_" + dashboardViewArgs.DataViewName + "_" + sequenceNumber;
		}
		return "panel_" + dashboardViewArgs.DataViewName;
	}

	public string GetRefreshButtonName(PanelCardGridArgs panelCardGridArgs)
	{
		return "btn_refresh_" + panelCardGridArgs.DashboardViewArgsList[0].DataViewName;
	}

	public string GetLayoutSearchButtonName(PanelCardGridArgs panelCardGridArgs)
	{
		return "btn_search_" + panelCardGridArgs.DashboardViewArgsList[0].DataViewName;
	}

	public string GetSearchableTrackerEventName(DashboardViewArgs dashboardViewArgs)
	{
		return "search_" + dashboardViewArgs.DataViewName;
	}

	public string GetInitializeSearchableTrackerEventName(DashboardViewArgs dashboardViewArgs)
	{
		return "search_initialize_" + dashboardViewArgs.DataViewName;
	}

	public string GetAddNewButtonName(DashboardViewArgs dashboardViewArgs)
	{
		return "btn_addNew_" + dashboardViewArgs.DataViewName;
	}

	public string GetPageSearchButtonName(DashboardViewArgs dashboardViewArgs)
	{
		return "btn_page_search_" + dashboardViewArgs.DataViewName;
	}

	public string GetResetPageSearchButtonName(DashboardViewArgs dashboardViewArgs)
	{
		return "btn_reset_page_search_" + dashboardViewArgs.DataViewName;
	}

	public string GetLayoutSearchButtonName(DashboardViewArgs dashboardViewArgs)
	{
		return "btn_search_" + dashboardViewArgs.DataViewName;
	}

	public string GetCustomActionButtonName(DashboardViewArgs dashboardViewArgs, string actionId)
	{
		return "btn_" + actionId + "_" + dashboardViewArgs.DataViewName;
	}

	public string GetDataviewNameForEmbedGrid(DashboardViewArgs dashboardViewArgs)
	{
		return dashboardViewArgs.DataViewName + "_EmbedGrid";
	}

	public DashboardViewArgs CreateDashboardViewArgs(DashboardDataSet dashboardDataSet, DashboardQuery dashboardQuery, DashboardView currentDashboardView, List<DashboardView> dashboardViews = null, bool promptTrackers = false)
	{
		DashboardViewArgs dashboardViewArgs = new DashboardViewArgs();
		dashboardViewArgs.DashboardQuery = dashboardQuery;
		dashboardViewArgs.DashboardView = currentDashboardView;
		dashboardViewArgs.IsTracker = currentDashboardView.ViewType == "Tracker";
		dashboardViewArgs.DataViewName = GetDataViewName(dashboardDataSet, dashboardQuery, currentDashboardView, dashboardViews, dashboardViewArgs.IsTracker);
		dashboardViewArgs.IsGrid = currentDashboardView.ViewType == "Grid";
		dashboardViewArgs.IsChart = currentDashboardView.ViewType == "Chart";
		dashboardViewArgs.IsGauge = currentDashboardView.ViewType == "Gauge";
		dashboardViewArgs.DashboardViewColumns = dashboardDataSet.DashboardViewColumns.Where((DashboardViewColumns dvc) => dvc.ViewID == currentDashboardView.ViewID && dvc.QueryID == dashboardQuery.QueryID).ToList();
		dashboardViewArgs.DashboardViewImageColumns = dashboardDataSet.DashboardViewImageColumns?.Where((DashboardViewImageColumns dvim) => dvim.ViewID == currentDashboardView.ViewID && dvim.QueryID == dashboardQuery.QueryID).ToList();
		if (dashboardDataSet.DashboardViewFilter != null)
		{
			dashboardViewArgs.DashboardViewFilters = dashboardDataSet.DashboardViewFilter.Where((DashboardViewFilter dvf) => dvf.ViewID == currentDashboardView.ViewID && dvf.QueryID == dashboardQuery.QueryID).ToList();
		}
		else
		{
			dashboardViewArgs.DashboardViewFilters = new List<DashboardViewFilter>();
		}
		if (dashboardViewArgs.IsChart)
		{
			DashboardChartSettings dashboardChartSettings = Deserialize<DashboardChartSettings>(currentDashboardView.ChartSettings);
			currentDashboardView.DasboardChart = GetChartSettings(dashboardQuery, currentDashboardView, dashboardViewArgs.DashboardViewColumns, dashboardViewArgs.DashboardViewFilters, dashboardChartSettings.ChartStyle.ChartType);
		}
		if (dashboardViewArgs.IsGauge)
		{
			DashboardViewOptions dashboardViewOptions = dashboardDataSet.DashboardViewOptions.FirstOrDefault((DashboardViewOptions x) => x.PropertyName.Equals("GaugeData") && x.ViewID == currentDashboardView.ViewID);
			if (dashboardViewOptions != null)
			{
				currentDashboardView.DashboardGauge = GetGaugeSettings(dashboardQuery, currentDashboardView, dashboardViewArgs.DashboardViewColumns, dashboardViewArgs.DashboardViewFilters, dashboardViewOptions);
			}
		}
		foreach (DashboardViewOptions item in dashboardDataSet.DashboardViewOptions?.Where((DashboardViewOptions p) => p.PropertyName == "AddNewSubscribers"))
		{
			DocumentElement documentElement = Deserialize<DocumentElement>(item.PropertyValue);
			if (documentElement?.AddNewSubscriber != null)
			{
				dashboardViewArgs.Subscribers.AddRange(documentElement.AddNewSubscriber);
			}
		}
		AddCallContextSubscribers(dashboardDataSet, dashboardViewArgs);
		IEnumerable<DashboardViewOptions> enumerable = dashboardDataSet.DashboardViewOptions?.Where((DashboardViewOptions qo) => qo.QueryID == dashboardQuery.QueryID && qo.ViewID == currentDashboardView.ViewID);
		if (enumerable != null)
		{
			foreach (DashboardViewOptions item2 in enumerable)
			{
				switch (item2.PropertyName.ToLower())
				{
				case "updatable":
					dashboardViewArgs.IsUpdatable = item2.PropertyValue.ToLower() == "true";
					break;
				case "gridgroupby":
					dashboardViewArgs.HasGridGroupBy = item2.PropertyValue.ToLower() == "true";
					break;
				case "allowaddnew":
					dashboardViewArgs.AllowAddNew = item2.PropertyValue.ToLower() == "true";
					if (dashboardViewArgs.AllowAddNew)
					{
						dashboardViewArgs.AddNewLabel = dashboardDataSet.DashboardQueryOptions.First((DashboardQueryOptions qo) => qo.PropertyName == "AddNewLabel" && qo.QueryID == dashboardQuery.QueryID).PropertyValue;
					}
					break;
				case "inputprompts":
					if (dashboardViewArgs.IsTracker)
					{
						dashboardViewArgs.IsSearchTrackerSlider = item2.PropertyValue.ToLower() == "true" || ((item2.PropertyValue.ToLower() == "false") & promptTrackers);
						dashboardViewArgs.IsSearchTracker = true;
					}
					break;
				case "viewbuttons":
					dashboardViewArgs.DocumentElement = Deserialize<DocumentElement>(item2.PropertyValue);
					break;
				case "trackerembedgrid":
					dashboardViewArgs.IsTrackerEmbedGrid = item2.PropertyValue.ToLower() == "true";
					break;
				case "gridcaption":
					if (item2.PropertyValue != null)
					{
						dashboardViewArgs.GridCaption = item2.PropertyValue;
					}
					break;
				}
			}
		}
		if (!dashboardViewArgs.IsSearchTrackerSlider && dashboardViewArgs.IsTracker && dashboardViewArgs.DashboardViewColumns.Any((DashboardViewColumns col) => col.DashboardPrompt))
		{
			dashboardViewArgs.IsSearchTracker = true;
		}
		return dashboardViewArgs;
	}

	private DashboardGauge GetGaugeSettings(DashboardQuery dashboardQuery, DashboardView dashboardView, List<DashboardViewColumns> dashboardViewColumns, List<DashboardViewFilter> dashboardViewFilters, DashboardViewOptions gaugeData)
	{
		DashboardGauge dashboardGauge = new DashboardGauge();
		dashboardGauge.BAQ = dashboardQuery.DynamicQueryID;
		dashboardGauge.DashboardDefinitionID = dashboardView.DefinitionID;
		dashboardGauge.GaugeType = dashboardView.ViewType;
		dashboardGauge.GaugeCaption = dashboardView.Caption;
		dashboardGauge.gaugeDataSet = Deserialize<GaugeDataSet>("<?xml version=\"1.0\" encoding=\"utf-16\"?>\n" + gaugeData.PropertyValue.Replace("<GaugeDataSet xmlns=\"http://tempuri.org/GaugeDataSet.xsd\">", "<GaugeDataSet>"));
		if (dashboardViewFilters.Count() > 0)
		{
			dashboardGauge.Filters = new List<DashboardChartFilter>();
			foreach (DashboardViewFilter dashboardViewFilter in dashboardViewFilters)
			{
				dashboardGauge.Filters.Add(new DashboardChartFilter
				{
					ColumnName = dashboardViewFilter.ColumnName,
					Condition = dashboardViewFilter.Condition,
					Value = dashboardViewFilter.Value,
					IsValueAnotherColumn = dashboardViewFilter.IsValueAnotherColumn
				});
			}
		}
		return dashboardGauge;
	}

	internal void InterpolateUrlParams(string dataViewId, string columnName, string token, DashboardURL urlWidget)
	{
		if (dataViewId != null && columnName != null)
		{
			urlWidget.URLAddress = Regex.Replace(urlWidget.URLAddress, token, "{" + dataViewId + "." + columnName + "}");
		}
	}

	internal List<QueryPhraseSubscribers> GetSubscriberList(string queryPhraseSubscribers)
	{
		return Deserialize<QueryPhraseSubscriberList>(queryPhraseSubscribers.Replace("DocumentElement", "QueryPhraseSubscriberList")).QueryPhraseSubscribers;
	}

	private static T Deserialize<T>(string xml)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		using StringReader textReader = new StringReader(xml);
		return (T)xmlSerializer.Deserialize(textReader);
	}

	public string GetValue(DashboardDataSet dashboardDataSet, DashboardView dashboardView, string columnName, string val)
	{
		string dataType = dashboardDataSet.DashboardViewColumns?.FirstOrDefault((DashboardViewColumns c) => c.QueryID == dashboardView.QueryID && c.ViewID == dashboardView.ViewID && c.ColumnName == columnName)?.DataType;
		return GetValue(dataType, val);
	}

	public string GetEpBindingValue(string dataType, string value, bool isRequired = false)
	{
		if (dataType == "datetime" || dataType == "date")
		{
			if (!isRequired)
			{
				return "??" + value;
			}
			return "?" + value;
		}
		if (!isRequired)
		{
			return "'??" + value + "'";
		}
		return "'?" + value + "'";
	}

	public string GetValue(string dataType, string val)
	{
		if (string.IsNullOrEmpty(dataType))
		{
			return "'" + val + "'";
		}
		switch (dataType.ToLower())
		{
		case "bit":
		case "int":
		case "decimal":
		case "integer":
		case "logical":
		case "bigint":
		case "datetime":
			return val;
		case "date":
		{
			if (DateTime.TryParse(val, out var _))
			{
				val = DateTime.Parse(val).ToString("yyyy-MM-dd");
			}
			return val;
		}
		default:
			return "'" + val + "'";
		}
	}

	public string GetDefaultValue(string dataType)
	{
		if (dataType == null)
		{
			return string.Empty;
		}
		switch (dataType.ToLower())
		{
		case "bigint":
		case "int":
		case "integer":
		case "decimal":
			return "0";
		case "bit":
		case "boolean":
			return "false";
		default:
			return string.Empty;
		}
	}

	public QueryField GetQueryField(string columnName, DynamicQueryDefinition dynamicQueryDefinition)
	{
		foreach (QueryField item in dynamicQueryDefinition.QueryField)
		{
			string tableID = item.TableID;
			string fieldName = item.FieldName;
			string dBTableName = item.DBTableName;
			if (columnName == tableID + "_" + fieldName || (tableID == dBTableName + "_UD" && columnName == dBTableName + "_" + fieldName))
			{
				return item;
			}
		}
		return null;
	}

	public List<PanelCardGridArgs> SplitPanelCardGrids(PanelCardGridArgs panelCardGridArgs)
	{
		List<PanelCardGridArgs> list = new List<PanelCardGridArgs>();
		if (panelCardGridArgs.DashboardViewArgsList.Count == 1 || !panelCardGridArgs.DashboardViewArgsList.Any((DashboardViewArgs dv) => dv.IsUpdatable) || panelCardGridArgs.DashboardViewArgsList.All((DashboardViewArgs dv) => dv.IsUpdatable))
		{
			list.Add(panelCardGridArgs);
			return list;
		}
		list.Add(Create(panelCardGridArgs, panelCardGridArgs.DashboardViewArgsList.Where((DashboardViewArgs dv) => !dv.IsUpdatable).ToList()));
		list.Add(Create(panelCardGridArgs, panelCardGridArgs.DashboardViewArgsList.Where((DashboardViewArgs dv) => dv.IsUpdatable).ToList()));
		return list;
	}

	public PanelCardGridArgs Create(PanelCardGridArgs panelCardGridArgs, List<DashboardViewArgs> dashboardViewArgsList)
	{
		PanelCardGridArgs panelCardGridArgs2 = new PanelCardGridArgs
		{
			DashboardQuery = panelCardGridArgs.DashboardQuery,
			DynamicQueryDefinition = panelCardGridArgs.DynamicQueryDefinition,
			SearchFilters = panelCardGridArgs.SearchFilters,
			ServerFilter = panelCardGridArgs.ServerFilter,
			SliderDashboardViews = panelCardGridArgs.SliderDashboardViews
		};
		panelCardGridArgs2.DashboardViewArgsList.AddRange(dashboardViewArgsList);
		panelCardGridArgs2.DashboardViewColumns = GetDashboardViewColumns(panelCardGridArgs2.DashboardViewArgsList);
		panelCardGridArgs2.DashboardViewImageColumns = GetDashboardViewImageColumns(panelCardGridArgs2.DashboardViewArgsList);
		return panelCardGridArgs2;
	}

	public List<DashboardViewColumns> GetDashboardViewColumns(List<DashboardViewArgs> dashboardViewArgsList)
	{
		List<DashboardViewColumns> list = new List<DashboardViewColumns>();
		foreach (DashboardViewArgs dashboardViewArgs in dashboardViewArgsList)
		{
			foreach (DashboardViewColumns dashboardViewColumn in dashboardViewArgs.DashboardViewColumns)
			{
				if (list.Find((DashboardViewColumns col) => col.ColumnName == dashboardViewColumn.ColumnName) == null)
				{
					list.Add(dashboardViewColumn);
				}
			}
		}
		return list;
	}

	public List<DashboardViewImageColumns> GetDashboardViewImageColumns(List<DashboardViewArgs> dashboardViewArgsList)
	{
		List<DashboardViewImageColumns> list = new List<DashboardViewImageColumns>();
		foreach (DashboardViewArgs dashboardViewArgs in dashboardViewArgsList)
		{
			if (dashboardViewArgs.DashboardViewImageColumns == null)
			{
				continue;
			}
			foreach (DashboardViewImageColumns dashboardViewColumn in dashboardViewArgs.DashboardViewImageColumns.OrderBy((DashboardViewImageColumns m) => m.VisibleIndex))
			{
				if (list.Find((DashboardViewImageColumns col) => col.ColumnName == dashboardViewColumn.ColumnName) == null)
				{
					list.Add(dashboardViewColumn);
				}
			}
		}
		return list;
	}

	public List<DashboardViewArgs> CreateDashboardViewArgsList(DashboardDataSet dashboardDataSet, DashboardQuery dashboardQuery, List<DashboardView> dashboardViews, bool prompTrackers = false)
	{
		List<DashboardViewArgs> list = new List<DashboardViewArgs>();
		foreach (DashboardView dashboardView in dashboardViews)
		{
			DashboardViewArgs dashboardViewArgs = CreateDashboardViewArgs(dashboardDataSet, dashboardQuery, dashboardView, dashboardViews, prompTrackers);
			list.Add(dashboardViewArgs);
			if (dashboardViewArgs.IsSearchTrackerSlider || !dashboardViewArgs.IsSearchTracker)
			{
				continue;
			}
			if (dashboardViewArgs.DashboardViewColumns.Any((DashboardViewColumns col) => col.DashboardPrompt && col.IsVisible))
			{
				DashboardView currentDashboardView = UpdateDashboardDataSetForSearch(dashboardDataSet, dashboardViewArgs);
				DashboardViewArgs item = CreateDashboardViewArgs(dashboardDataSet, dashboardQuery, currentDashboardView);
				list.Add(item);
			}
			dashboardViewArgs.IsSearchTracker = false;
			foreach (DashboardViewColumns dashboardViewColumn in dashboardViewArgs.DashboardViewColumns)
			{
				dashboardViewColumn.DashboardPrompt = false;
			}
		}
		return list;
	}

	private DashboardView UpdateDashboardDataSetForSearch(DashboardDataSet dashboardDataSet, DashboardViewArgs trackerDashboardViewArgs)
	{
		DashboardView dashboardView = Clone(trackerDashboardViewArgs.DashboardView);
		dashboardView.ViewID = Guid.NewGuid().ToString();
		List<DashboardView> list = dashboardDataSet.DashboardView.ToList();
		list.Add(dashboardView);
		dashboardDataSet.DashboardView = list.ToArray();
		if (trackerDashboardViewArgs.DashboardViewColumns.Any())
		{
			List<DashboardViewColumns> list2 = Clone(trackerDashboardViewArgs.DashboardViewColumns.Where((DashboardViewColumns col) => col.DashboardPrompt).ToList());
			foreach (DashboardViewColumns item in list2)
			{
				item.ViewID = dashboardView.ViewID;
			}
			List<DashboardViewColumns> list3 = dashboardDataSet.DashboardViewColumns.ToList();
			list3.AddRange(list2);
			dashboardDataSet.DashboardViewColumns = list3.ToArray();
		}
		if (trackerDashboardViewArgs.DashboardViewFilters.Any())
		{
			List<DashboardViewFilter> list4 = Clone(trackerDashboardViewArgs.DashboardViewFilters.ToList());
			foreach (DashboardViewFilter item2 in list4)
			{
				item2.ViewID = dashboardView.ViewID;
			}
			List<DashboardViewFilter> list5 = dashboardDataSet.DashboardViewFilter.ToList();
			list5.AddRange(list4);
			dashboardDataSet.DashboardViewFilter = list5.ToArray();
		}
		List<DashboardViewOptions> list6 = ((dashboardDataSet.DashboardViewOptions == null) ? new List<DashboardViewOptions>() : dashboardDataSet.DashboardViewOptions.ToList());
		list6.Add(new DashboardViewOptions
		{
			PropertyName = "inputprompts",
			PropertyValue = "true",
			QueryID = dashboardView.QueryID,
			ViewID = dashboardView.ViewID
		});
		dashboardDataSet.DashboardViewOptions = list6.ToArray();
		return dashboardView;
	}

	private static T Clone<T>(T source)
	{
		return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
	}

	public string[] GetImageColumns(DashboardViewArgs dashboardViewArgs)
	{
		List<string> columns = new List<string>();
		dashboardViewArgs.DashboardViewImageColumns.ForEach(delegate(DashboardViewImageColumns x)
		{
			columns.Add(x.ColumnName);
		});
		return columns.ToArray();
	}

	public GroupInfo GetGroupsColumns(DashboardViewArgs dashboardViewArgs, DashboardArgs args)
	{
		DashboardViewOptions dashboardViewOptions = args.DashboardDataSet.DashboardViewOptions?.FirstOrDefault((DashboardViewOptions qo) => qo.PropertyName == "GridLayout" && qo.QueryID == dashboardViewArgs.DashboardQuery.QueryID && qo.ViewID == dashboardViewArgs.DashboardView.ViewID);
		if (dashboardViewOptions != null)
		{
			XDocument value = XDocument.Parse(RemoveAllNamespaces(XElement.Parse(dashboardViewOptions.PropertyValue)).ToString());
			StringBuilder stringBuilder = new StringBuilder();
			JsonSerializer.Create().Serialize(new CustomJsonWriter(new StringWriter(stringBuilder)), value);
			DashboardGridDisplayLayout.GridLayout gridLayout = JsonConvert.DeserializeObject<DashboardGridDisplayLayout.GridLayout>(stringBuilder.ToString());
			if (gridLayout != null && gridLayout.GridLayoutDetail != null)
			{
				DashboardGridDisplayLayout.Body model = gridLayout.GridLayoutDetail.Model;
				if (model != null && model.SerializedColumnID != null)
				{
					DashboardGridDisplayLayout.UltraGridDisplayLayout ultraGridDisplayLayout = model.UltraGridDisplayLayout;
					if (ultraGridDisplayLayout != null && ultraGridDisplayLayout.ViewStyleBand == 2)
					{
						bool flag = false;
						List<Epicor.MetaFX.Core.Models.Layout.ErpControls.Group> list = new List<Epicor.MetaFX.Core.Models.Layout.ErpControls.Group>();
						foreach (DashboardGridDisplayLayout.SerializedColumnID item in model.SerializedColumnID)
						{
							if (item.IsGroupByColumn)
							{
								string columnKey = item.Key.Href.Replace('#', ' ').Trim();
								string field = model.UltraGridColumn?.FirstOrDefault((DashboardGridDisplayLayout.UltraGridColumn e) => e.Key.Id == columnKey)?.Key.Text;
								flag = true;
								list.Add(new Epicor.MetaFX.Core.Models.Layout.ErpControls.Group
								{
									Field = field
								});
							}
						}
						if (flag)
						{
							groupInfo = new GroupInfo
							{
								QueryID = dashboardViewArgs.DashboardView.QueryID,
								ViewID = dashboardViewArgs.DashboardView.ViewID,
								Groups = list
							};
						}
					}
				}
			}
		}
		return groupInfo;
	}

	public void AddCallContextSubscribers(DashboardDataSet dashboardDataSet, DashboardViewArgs args)
	{
		foreach (DashboardQueryOptions item in dashboardDataSet.DashboardQueryOptions?.Where((DashboardQueryOptions p) => p.PropertyName.Equals("CallContextSubscribers", StringComparison.OrdinalIgnoreCase)))
		{
			DocumentElement documentElement = Deserialize<DocumentElement>(item.PropertyValue);
			for (int num = 0; num < documentElement.CallContextSubscribers.Count; num++)
			{
				CallContextSubscriber callContextSubscriber = documentElement.CallContextSubscribers[num];
				if (string.IsNullOrEmpty(callContextSubscriber.PublishColumn) || string.IsNullOrEmpty(callContextSubscriber.BPMDataColumn))
				{
					documentElement.CallContextSubscribers.RemoveAt(num);
					num--;
				}
			}
			args.CallContextSubscribers.AddRange(documentElement.CallContextSubscribers);
		}
	}

	public static string RemoveAllNamespaces(string xmlDocument)
	{
		return RemoveAllNamespaces(XElement.Parse(xmlDocument)).ToString();
	}

	private static XElement RemoveAllNamespaces(XElement e)
	{
		foreach (XElement item in e.DescendantsAndSelf())
		{
			item.Name = item.Name.LocalName;
			item.ReplaceAttributes(from xattrib in item.Attributes()
				where !xattrib.IsNamespaceDeclaration
				select new XAttribute(xattrib.Name.LocalName, xattrib.Value));
		}
		return e;
	}

	private DashboardChart GetChartSettings(DashboardQuery dashboardQuery, DashboardView dashboardView, List<DashboardViewColumns> dashboardViewColumns, List<DashboardViewFilter> dashboardViewFilters, string chartType)
	{
		DashboardChart dashboardChart = new DashboardChart();
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		list.AddRange(from y in dashboardViewColumns
			where y.OnXAxis
			select y.ColumnName);
		list2.AddRange(from y in dashboardViewColumns
			where y.OnYAxis
			select y.ColumnName);
		list3.AddRange(from y in dashboardViewColumns
			where y.OnZAxis
			select y.ColumnName);
		dashboardChart.XAxis = list;
		dashboardChart.YAxis = list2;
		dashboardChart.ZAxis = list3;
		dashboardChart.BAQ = dashboardQuery.DynamicQueryID;
		dashboardChart.DashboardDefinitionID = dashboardView.DefinitionID;
		dashboardChart.ChartType = chartType;
		dashboardChart.ChartCaption = dashboardView.Caption;
		if (dashboardViewFilters.Count() > 0)
		{
			dashboardChart.Filters = new List<DashboardChartFilter>();
			foreach (DashboardViewFilter dashboardViewFilter in dashboardViewFilters)
			{
				dashboardChart.Filters.Add(new DashboardChartFilter
				{
					ColumnName = dashboardViewFilter.ColumnName,
					Condition = dashboardViewFilter.Condition,
					Value = dashboardViewFilter.Value,
					IsValueAnotherColumn = dashboardViewFilter.IsValueAnotherColumn
				});
			}
		}
		return dashboardChart;
	}
}
