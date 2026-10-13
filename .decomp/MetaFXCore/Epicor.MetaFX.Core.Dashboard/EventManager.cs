using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Dashboard;

internal class EventManager
{
	private readonly DashboardArgs dashboardArgs;

	private readonly DashboardHelper dashboardHelper;

	private readonly HelperMethods helperMethods;

	private readonly BAQWhereItemHelper baqWhereItemHelper;

	private readonly DashboardDataSet dashboardDataSet;

	private Action<MessageDetail> logMessageDetail;

	public EventManager(DashboardArgs dashboardArgs, Action<MessageDetail> _logMessageDetail)
	{
		logMessageDetail = _logMessageDetail;
		this.dashboardArgs = dashboardArgs;
		dashboardHelper = dashboardArgs.DashboardHelper;
		helperMethods = dashboardArgs.HelperMethods;
		baqWhereItemHelper = new BAQWhereItemHelper(dashboardHelper);
		dashboardDataSet = dashboardArgs.DashboardDataSet;
	}

	public EventSchema GenerateSchema()
	{
		EventSchema eventSchema = new EventSchema();
		eventSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1.1/events.json";
		eventSchema.ContentVersion = "2.0";
		eventSchema.Imports = new object[1]
		{
			new
			{
				events = new Reference
				{
					Refs = dashboardArgs.ImportPath + "/events.jsonc#/events"
				}
			}
		};
		return eventSchema;
	}

	public List<Event> GenerateInitialEvents(List<DashboardViewArgs> searchableSliders, List<PanelCardGridArgs> panelCardGridArgs, List<DashboardViewArgs> sliderSearchPanels, Dictionary<string, List<RowColumnChangedArgs>> rowChangedEventList)
	{
		List<Event> list = new List<Event>();
		Event obj = CreateWindowOnLoadEvent(searchableSliders, panelCardGridArgs);
		if (obj != null)
		{
			list.Add(obj);
		}
		Event obj2 = CreateTrackerEvent(sliderSearchPanels);
		if (obj2 != null)
		{
			list.Add(obj2);
		}
		list.Add(CreateRefreshAllEvent(panelCardGridArgs, rowChangedEventList));
		return list;
	}

	public List<Event> GenerateEvents(PanelCardGridArgs panelCardGridArgs, DashboardDataSet dashboardDataSet, DataViewSchema dataViewSchemma, DashboardQuery dashboardQuery)
	{
		List<Event> list = new List<Event>();
		foreach (DashboardViewArgs dashboardViewArgs in panelCardGridArgs.DashboardViewArgsList)
		{
			if (dashboardViewArgs.DocumentElement?.ViewButtons == null)
			{
				continue;
			}
			foreach (ViewButtons viewButton in dashboardViewArgs.DocumentElement.ViewButtons)
			{
				list.Add(CreateEventForCustomAction(dashboardViewArgs, viewButton.Action));
			}
		}
		if (panelCardGridArgs.DashboardViewArgsList.Any((DashboardViewArgs al) => al.IsUpdatable))
		{
			list.Add(CreateEventForUpdateAction(panelCardGridArgs, "Update"));
		}
		if (panelCardGridArgs.DashboardViewArgsList.Any((DashboardViewArgs al) => al.AllowAddNew))
		{
			list.Add(CreateEventForAddAction(dataViewSchemma, dashboardDataSet, panelCardGridArgs, "Add"));
		}
		list.Add(CreateRefreshEvent(panelCardGridArgs));
		if (panelCardGridArgs.SliderDashboardViews.Any())
		{
			foreach (DashboardViewArgs sliderDashboardView in panelCardGridArgs.SliderDashboardViews)
			{
				string searchableTrackerPageName = dashboardHelper.GetSearchableTrackerPageName(dashboardArgs.DashboardDataSet, panelCardGridArgs.DashboardQuery, sliderDashboardView.DashboardView);
				list.Add(CreateEventToNavigateToSearchPage(dashboardHelper.GetLayoutSearchButtonName(sliderDashboardView), searchableTrackerPageName, searchableTrackerPageName));
			}
		}
		return list;
	}

	public List<Event> GenerateEmbedGridEvents(PanelCardGridArgs embedPanelCardGridArgs)
	{
		List<Event> list = new List<Event>();
		list.Add(CreateRefreshEvent(embedPanelCardGridArgs));
		if (embedPanelCardGridArgs.SliderDashboardViews.Any())
		{
			foreach (DashboardViewArgs sliderDashboardView in embedPanelCardGridArgs.SliderDashboardViews)
			{
				string searchableTrackerPageName = dashboardHelper.GetSearchableTrackerPageName(dashboardArgs.DashboardDataSet, embedPanelCardGridArgs.DashboardQuery, sliderDashboardView.DashboardView);
				list.Add(CreateEventToNavigateToSearchPage(dashboardHelper.GetLayoutSearchButtonName(sliderDashboardView), searchableTrackerPageName, searchableTrackerPageName));
			}
		}
		return list;
	}

	public List<Event> GenerateProcessLinkEvents(string fileName, DashboardDataSet dashboardDataSet)
	{
		List<Event> list = new List<Event>();
		if (dashboardDataSet.DashboardProcess != null)
		{
			DashboardProcess[] dashboardProcess = dashboardDataSet.DashboardProcess;
			foreach (DashboardProcess dashboardProcess2 in dashboardProcess)
			{
				list.Add(CreateEventToOpenApp(dashboardProcess2.DefinitionID, dashboardProcess2.MenuItemID, fileName));
			}
		}
		return list;
	}

	public List<Event> GenerateEvents(DashboardViewArgs dashboardViewArgs, List<DashboardView> gridViews, Dictionary<string, List<RowColumnChangedArgs>> rowColumnChangedEventList)
	{
		DashboardQuery dashboardQuery = dashboardViewArgs.DashboardQuery;
		List<Event> list = new List<Event>();
		if (dashboardViewArgs.IsTracker)
		{
			if (!dashboardViewArgs.IsSearchTrackerSlider)
			{
				foreach (DashboardView gridView in gridViews)
				{
					string dataViewName = dashboardHelper.GetDataViewName(dashboardArgs.DashboardDataSet, dashboardQuery, gridView);
					if (!rowColumnChangedEventList.ContainsKey(dataViewName))
					{
						rowColumnChangedEventList.Add(dataViewName, new List<RowColumnChangedArgs>());
					}
					rowColumnChangedEventList[dataViewName].Add(new RowColumnChangedArgs
					{
						Type = RowColumnChangedType.copyRow,
						FromDataView = dataViewName,
						ToDataView = dashboardViewArgs.DataViewName
					});
				}
			}
			if (dashboardViewArgs.IsSearchTracker)
			{
				list.Add(CreateEventToInitializeSearchPage(dashboardViewArgs));
				if (dashboardViewArgs.IsSearchTrackerSlider)
				{
					list.Add(CreateButtonEvent(dashboardHelper.GetResetPageSearchButtonName(dashboardViewArgs), dashboardHelper.GetInitializeSearchableTrackerEventName(dashboardViewArgs)));
				}
			}
		}
		return list;
	}

	public List<BAQWhereExpression> GetSearchFilter(DashboardViewArgs dashboardViewArgs, PanelCardGridArgs panelCardGridArgs)
	{
		if (dashboardViewArgs.IsSearchTracker)
		{
			Queue<BAQWhereItem> trackerWhereClause = GetTrackerWhereClause(dashboardViewArgs, panelCardGridArgs);
			if (trackerWhereClause.Count > 0)
			{
				return baqWhereItemHelper.GetSearchTrackerFilters(trackerWhereClause, dashboardDataSet, dashboardViewArgs.DashboardView);
			}
		}
		return null;
	}

	public List<BAQWhereExpression> GetFilters(DashboardViewArgs dashboardViewArgs, Dictionary<string, List<RowColumnChangedArgs>> rowColumnChangedEventList, PanelCardGridArgs panelCardGridArgs)
	{
		Queue<BAQWhereItem> queue = new Queue<BAQWhereItem>();
		List<DashboardViewFilter> dashboardViewFilters = dashboardViewArgs.DashboardViewFilters;
		if (dashboardViewFilters == null)
		{
			return GetBAQExpressions(queue);
		}
		new StringBuilder();
		List<DashboardQueryPublishers> list = new List<DashboardQueryPublishers>();
		foreach (DashboardViewFilter item in dashboardViewFilters)
		{
			string empty = string.Empty;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool isEpBinding = false;
			string columnName = item.ColumnName;
			string text = item.Condition;
			string value = item.Value;
			string tableName = string.Empty;
			string columnName2 = string.Empty;
			QueryField queryField = dashboardHelper.GetQueryField(columnName, panelCardGridArgs.DynamicQueryDefinition);
			if (queryField != null)
			{
				tableName = queryField.TableID;
				columnName2 = queryField.FieldName;
			}
			if (item.IsPublisher)
			{
				string[] array = item.Value.Split(new char[1] { ':' });
				string publisherName = array[0];
				string publishedColumn = array[1];
				DashboardQueryPublishers queryPublisher = dashboardDataSet.DashboardQueryPublishers.FirstOrDefault((DashboardQueryPublishers qb) => qb.PublisherName == publisherName && qb.PublishedColumn == publishedColumn.Trim());
				if (queryPublisher == null)
				{
					logMessageDetail(new MessageDetail
					{
						Message = "Publisher '" + publisherName + "' and PublishedColumn '" + publishedColumn + "' does not exists.",
						Type = "warning"
					});
					continue;
				}
				list.Add(queryPublisher);
				foreach (DashboardView item2 in dashboardDataSet.DashboardView.Where((DashboardView dav) => dav.QueryID == queryPublisher.QueryID))
				{
					string dataViewName = dashboardHelper.GetDataViewName(dashboardArgs.DashboardDataSet, dashboardDataSet.DashboardQuery.First((DashboardQuery dq) => dq.QueryID == queryPublisher.QueryID), item2);
					if (!rowColumnChangedEventList.ContainsKey(dataViewName))
					{
						rowColumnChangedEventList.Add(dataViewName, new List<RowColumnChangedArgs>());
					}
					rowColumnChangedEventList[dataViewName].Add(new RowColumnChangedArgs
					{
						Type = RowColumnChangedType.updateRow,
						EpBinding = "TransView." + dashboardViewArgs.DataViewName + "_" + publishedColumn.Trim() + "_Filter",
						Value = "{" + dataViewName + "." + publishedColumn.Trim() + "}"
					});
				}
				empty = "??{TransView." + dashboardViewArgs.DataViewName + "_" + publishedColumn.Trim() + "_Filter}";
				isEpBinding = true;
			}
			else
			{
				empty = value;
			}
			if (empty.Length > 0 && empty.StartsWith("Constant: "))
			{
				string text2 = empty.Split(new char[1] { ':' })[1];
				empty = "??{Constant." + text2.Trim() + "}";
				isEpBinding = true;
			}
			if (!(flag2 | flag3) && (!((empty.Length > 0) | flag) || !(empty != "DashboardNull")))
			{
				continue;
			}
			if (flag3)
			{
				empty = string.Empty;
			}
			_ = string.Empty;
			if (empty.Length > 0 && text.Equals("BEGINS", StringComparison.OrdinalIgnoreCase) && !item.IsValueAnotherColumn)
			{
				text = "LIKE";
				empty += "%";
			}
			if (text.Equals("BEGINS", StringComparison.OrdinalIgnoreCase))
			{
				text = ">=";
			}
			if (flag2)
			{
				string text3 = " = ";
				if (text != "=")
				{
					text3 = "<>";
				}
				_ = "IsNull(" + columnName + ", '1/1/1900')" + text3 + "'1/1/1900'";
			}
			else if (item.IsValueAnotherColumn)
			{
				queue.Enqueue(baqWhereItemHelper.GetClientBAQWhereItem(columnName2, tableName, text, empty, compareColumns: true, isEpBinding, columnName));
			}
			else
			{
				queue.Enqueue(baqWhereItemHelper.GetClientBAQWhereItem(columnName2, tableName, text, dashboardHelper.GetValue(dashboardArgs.DashboardDataSet, dashboardViewArgs.DashboardView, columnName, empty), compareColumns: false, isEpBinding, columnName));
			}
		}
		return GetBAQExpressions(queue);
	}

	public List<BAQWhereExpression> GetWhereClauses(PanelCardGridArgs panelCardGridArgs, Dictionary<string, List<RowColumnChangedArgs>> rowColumnChangedEventList)
	{
		List<DashboardQueryWhereClause> list = dashboardDataSet.DashboardQueryWhereClause?.Where((DashboardQueryWhereClause qr) => qr.QueryID == panelCardGridArgs.DashboardQuery.QueryID).ToList();
		Queue<BAQWhereItem> queue = new Queue<BAQWhereItem>();
		if (list == null)
		{
			return GetBAQExpressions(queue);
		}
		bool flag = false;
		bool isEpBinding = false;
		foreach (DashboardQueryWhereClause item in list)
		{
			string columnName = item.ColumnName;
			string condition = item.Condition;
			string value = item.Value;
			string empty = string.Empty;
			flag = false;
			if (item.ValueIsColumn == "True")
			{
				flag = true;
			}
			string tableName = string.Empty;
			string columnName2 = string.Empty;
			string dataType = string.Empty;
			QueryField queryField = dashboardHelper.GetQueryField(columnName, panelCardGridArgs.DynamicQueryDefinition);
			if (queryField != null)
			{
				tableName = queryField.TableID;
				columnName2 = queryField.FieldName;
				dataType = queryField.DataType;
			}
			if (!item.IsPublisher)
			{
				empty = ((!(value == "Dashboard Browse")) ? value : string.Empty);
			}
			else
			{
				string publisherName = value.Split(new char[1] { ':' })[0];
				string publishedColumn = value.Split(new char[1] { ':' })[1];
				DashboardQueryPublishers queryPublisher = dashboardDataSet.DashboardQueryPublishers.FirstOrDefault((DashboardQueryPublishers qb) => qb.PublisherName == publisherName && qb.PublishedColumn == publishedColumn.Trim());
				if (queryPublisher == null)
				{
					logMessageDetail(new MessageDetail
					{
						Message = "Publisher '" + publisherName + "' and PublishedColumn '" + publishedColumn + "' does not exists.",
						Type = "warning"
					});
					continue;
				}
				foreach (DashboardView item2 in dashboardDataSet.DashboardView.Where((DashboardView dav) => dav.QueryID == queryPublisher.QueryID))
				{
					DashboardQuery dashboardQuery = dashboardDataSet.DashboardQuery.First((DashboardQuery dq) => dq.QueryID == queryPublisher.QueryID);
					string dataViewName = dashboardHelper.GetDataViewName(dashboardArgs.DashboardDataSet, dashboardQuery, item2);
					if (!rowColumnChangedEventList.ContainsKey(dataViewName))
					{
						rowColumnChangedEventList.Add(dataViewName, new List<RowColumnChangedArgs>());
					}
					if (!rowColumnChangedEventList[dataViewName].Where((RowColumnChangedArgs arg) => arg.NextEvent == dashboardHelper.GetRefreshEventName(panelCardGridArgs)).Any())
					{
						rowColumnChangedEventList[dataViewName].Add(new RowColumnChangedArgs
						{
							Type = RowColumnChangedType.WhereClause,
							NextEvent = dashboardHelper.GetRefreshEventName(panelCardGridArgs)
						});
					}
					rowColumnChangedEventList[dataViewName].Add(new RowColumnChangedArgs
					{
						Type = RowColumnChangedType.updateRow,
						EpBinding = "TransView." + dashboardHelper.GetTransViewName(panelCardGridArgs, publishedColumn.Trim()),
						Value = "{" + dataViewName + "." + publishedColumn.Trim() + "}"
					});
					foreach (CallContextSubscriber callContextSubscriber in panelCardGridArgs.DashboardViewArgsList[0].CallContextSubscribers)
					{
						rowColumnChangedEventList[dataViewName].Add(new RowColumnChangedArgs
						{
							Type = RowColumnChangedType.updateRow,
							EpBinding = "CallContextBpmData." + callContextSubscriber.BPMDataColumn.Trim(),
							Value = "{" + dataViewName + "." + callContextSubscriber.PublishColumn.Trim() + "}"
						});
					}
				}
				empty = "{TransView." + dashboardHelper.GetTransViewName(panelCardGridArgs, publishedColumn.Trim()) + "}";
				isEpBinding = true;
			}
			if (empty.Length > 0 && empty.StartsWith("Constant: "))
			{
				string text = empty.Split(new char[1] { ':' })[1];
				empty = "{Constant." + text.Trim() + "}";
				isEpBinding = true;
			}
			if (empty.Length > 0 && empty != "DashboardNull")
			{
				queue.Enqueue(baqWhereItemHelper.GetBAQWhereItem(columnName2, tableName, condition, empty, flag, isEpBinding, dataType, columnName, honorNull: false, isRequired: true));
			}
		}
		return GetBAQExpressions(queue);
	}

	public void CreateFile(EventSchema eventSchema)
	{
		JObject data = dashboardArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events");
		helperMethods.CreateJsonFile(data, dashboardArgs.OutputPath, "events.jsonc");
	}

	private Event CreateWindowOnLoadEvent(List<DashboardViewArgs> searchableTracker, List<PanelCardGridArgs> panelCardGridArgsList)
	{
		List<object> timerEventActions = GetTimerEventActions(panelCardGridArgsList);
		foreach (DashboardViewArgs item in searchableTracker)
		{
			timerEventActions.Add(CreateNextEventAction(dashboardHelper.GetInitializeSearchableTrackerEventName(item)));
		}
		timerEventActions.Add(CreateNextEventAction("executeRefreshAll"));
		if (timerEventActions == null || timerEventActions.Count == 0)
		{
			return null;
		}
		return new Event
		{
			Name = "window",
			Trigger = new Trigger
			{
				Hook = "onLoad",
				Type = "Window"
			},
			Action = new
			{
				type = "condition",
				param = new
				{
					expression = "true",
					onSuccess = new
					{
						actions = timerEventActions.ToArray()
					}
				}
			}
		};
	}

	internal Event CreateTrackerEvent(List<DashboardViewArgs> sliderSearchPanels)
	{
		Event obj = new Event();
		if (sliderSearchPanels.Count > 0)
		{
			object[] actions = new object[1] { CreateNextEventAction(dashboardHelper.GetInitializeSearchableTrackerEventName(sliderSearchPanels[0])) };
			obj.Name = "BeforeInitialize";
			obj.Action = new
			{
				type = "condition",
				param = new
				{
					expression = "true",
					onSuccess = new { actions }
				}
			};
		}
		return obj;
	}

	private Event CreateRefreshEvent(PanelCardGridArgs panelCardGridArgs)
	{
		Event obj = new Event();
		obj.Name = dashboardHelper.GetRefreshEventName(panelCardGridArgs);
		obj.Action = new
		{
			type = "setProperty",
			param = new
			{
				component = panelCardGridArgs.ComponentId,
				props = new object[1]
				{
					new
					{
						prop = "invalidated",
						value = "true"
					}
				}
			}
		};
		return obj;
	}

	private Event CreateButtonEvent(string control, string callingEvent)
	{
		return new Event
		{
			Name = control,
			Trigger = new Trigger
			{
				Hook = "onClick",
				Type = "Control",
				Target = control
			},
			Action = new
			{
				type = "nextEvent",
				value = callingEvent
			}
		};
	}

	private Event CreateEventToInitializeSearchPage(DashboardViewArgs dashboardViewArgs)
	{
		Event obj = new Event();
		obj.Name = dashboardHelper.GetInitializeSearchableTrackerEventName(dashboardViewArgs);
		obj.Action = new
		{
			type = "updateRow",
			param = new object[1]
			{
				new
				{
					columns = UpdateColumns(dashboardViewArgs.DashboardQuery, dashboardViewArgs.DashboardView, dashboardViewArgs.DashboardViewColumns.Where((DashboardViewColumns dv) => dv.DashboardPrompt).ToList()).ToArray()
				}
			}
		};
		return obj;
	}

	private Event CreateEventToNavigateToSearchPage(string control, string pageName, string searchTrackerProgamName)
	{
		List<object> list = new List<object>();
		Event obj = new Event
		{
			Name = control,
			Trigger = new Trigger
			{
				Hook = "onClick",
				Type = "Control",
				Target = control
			}
		};
		string callingEvent = "search_initialize_" + searchTrackerProgamName;
		list.Add(CreateNextEventAction(callingEvent));
		var item = new
		{
			type = "open-slider",
			param = new
			{
				page = pageName
			}
		};
		list.Add(item);
		obj.Action = new
		{
			type = "condition",
			param = new
			{
				expression = "true",
				onSuccess = new
				{
					actions = list.ToArray()
				}
			}
		};
		return obj;
	}

	private Event CreateEventToOpenApp(string control, string menuId, string fileName)
	{
		Event obj = new Event();
		Trigger trigger = new Trigger();
		obj.Name = control;
		trigger.Hook = "onClick";
		trigger.Type = "Control";
		trigger.Target = "ToolRedirectTo" + menuId;
		obj.Trigger = trigger;
		obj.Action = new
		{
			type = "app-open",
			param = new
			{
				view = menuId,
				whoLaunched = "Kinetic." + fileName,
				launchOptions = new
				{
					type = "Ice.Lib.Framework.LaunchFormOptions",
					options = new
					{
						IsModal = false,
						SuppressFormSearch = true
					}
				}
			}
		};
		return obj;
	}

	private List<object> UpdateColumns(DashboardQuery dashboardQuery, DashboardView dashboardView, List<DashboardViewColumns> columnsToBeInitialized)
	{
		List<object> list = new List<object>();
		foreach (DashboardViewColumns item in columnsToBeInitialized)
		{
			list.Add(new
			{
				epBinding = dashboardHelper.GetDataViewName(dashboardDataSet, dashboardQuery, dashboardView) + "." + item.ColumnName,
				value = ((item.DataType == "date") ? null : string.Empty)
			});
		}
		return list;
	}

	public Event CreateRowChangedEvent(string dataview, List<RowColumnChangedArgs> argsList)
	{
		List<object> list = new List<object>();
		List<object> list2 = new List<object>();
		foreach (RowColumnChangedArgs item4 in argsList.Where((RowColumnChangedArgs arg) => arg.Type == RowColumnChangedType.updateRow))
		{
			var item = new
			{
				epBinding = item4.EpBinding,
				value = item4.Value
			};
			list2.Add(item);
		}
		var item2 = new
		{
			type = "updateRow",
			param = new object[1]
			{
				new
				{
					columns = list2.ToArray()
				}
			}
		};
		list.Add(item2);
		foreach (RowColumnChangedArgs item5 in argsList.Where((RowColumnChangedArgs arg) => arg.Type == RowColumnChangedType.copyRow))
		{
			var item3 = new
			{
				type = "copyRow",
				param = new
				{
					fromDataView = item5.FromDataView,
					toDataView = item5.ToDataView,
					setCurrentRow = true,
					loadData = true
				}
			};
			list.Add(item3);
		}
		return new Event
		{
			Name = dataview + "_RowChanged",
			Trigger = new Trigger
			{
				Type = "DataView",
				Target = dataview,
				Hook = "rowChanged"
			},
			Action = new
			{
				type = "condition",
				param = new
				{
					expression = "true",
					onSuccess = new
					{
						actions = list.ToArray()
					}
				}
			}
		};
	}

	private Event CreateRefreshAllEvent(List<PanelCardGridArgs> panelCardGridArgs, Dictionary<string, List<RowColumnChangedArgs>> rowChangedEventList)
	{
		return new Event
		{
			Name = "executeRefreshAll",
			Action = new
			{
				type = "condition",
				param = new
				{
					expression = "true",
					onSuccess = new
					{
						actions = GetRefreshAllEventActions(panelCardGridArgs, rowChangedEventList)
					}
				}
			}
		};
	}

	private List<object> RowUpdateBeforeRefreshAll(Dictionary<string, List<RowColumnChangedArgs>> rowChangedEventList)
	{
		List<object> subscriptions = new List<object>();
		rowChangedEventList.ToList().ForEach(delegate(KeyValuePair<string, List<RowColumnChangedArgs>> kvp)
		{
			subscriptions.AddRange(from rowColumnChangeArgs in kvp.Value
				where rowColumnChangeArgs.Type == RowColumnChangedType.updateRow
				select new
				{
					epBinding = rowColumnChangeArgs.EpBinding,
					value = (string)null
				});
		});
		if (subscriptions.Count > 0)
		{
			List<object> list = new List<object>();
			list.Add(new
			{
				type = "row-update",
				param = new object[1]
				{
					new
					{
						columns = subscriptions.ToArray()
					}
				}
			});
			return list;
		}
		return new List<object>();
	}

	private object[] GetRefreshAllEventActions(List<PanelCardGridArgs> panelCardGridArgs, Dictionary<string, List<RowColumnChangedArgs>> rowChangedEventList)
	{
		List<object> list = RowUpdateBeforeRefreshAll(rowChangedEventList);
		for (int i = 0; i < panelCardGridArgs.Count; i++)
		{
			var item = new
			{
				type = "nextEvent",
				value = dashboardHelper.GetRefreshEventName(panelCardGridArgs[i])
			};
			list.Add(item);
		}
		return list.ToArray();
	}

	private List<BAQWhereExpression> GetBAQExpressions(Queue<BAQWhereItem> baqWhereItems)
	{
		List<BAQWhereExpression> list = new List<BAQWhereExpression>();
		Queue<BAQWhereItem> queue = new Queue<BAQWhereItem>(baqWhereItems);
		while (queue.Count > 0)
		{
			BAQWhereItem bAQWhereItem = queue.Dequeue();
			BAQWhereExpression bAQWhereExpression = new BAQWhereExpression();
			bAQWhereExpression.Value = bAQWhereItem.FilterValue;
			bAQWhereExpression.Column = bAQWhereItem.BAQColumn;
			bAQWhereExpression.CompareColumns = bAQWhereItem.CompareColumns;
			string filterValue = bAQWhereItem.FilterValue;
			if ((filterValue == null || filterValue.IndexOf("Constant.NullDate") != -1) && bAQWhereItem.Value == null)
			{
				bAQWhereExpression.Condition = "nullDate";
			}
			else
			{
				bAQWhereExpression.Condition = bAQWhereItem.Condition;
			}
			bAQWhereExpression.HonorNull = bAQWhereItem.HonorNull;
			list.Add(bAQWhereExpression);
		}
		return list;
	}

	private List<object> GetTimerEventActions(List<PanelCardGridArgs> panelCardGridArgsList)
	{
		List<object> list = new List<object>();
		for (int i = 0; i < panelCardGridArgsList.Count; i++)
		{
			DashboardQuery dashboardQuery = panelCardGridArgsList[i].DashboardQuery;
			if (dashboardQuery.AutoRefreshOnLoad)
			{
				var item = new
				{
					type = "nextEvent",
					value = dashboardHelper.GetRefreshEventName(panelCardGridArgsList[i])
				};
				list.Add(item);
			}
			if (dashboardQuery.RefreshInterval > 0)
			{
				var item2 = new
				{
					type = "setTimer",
					param = new
					{
						interval = dashboardQuery.RefreshInterval * 1000,
						@event = dashboardHelper.GetRefreshEventName(panelCardGridArgsList[i])
					}
				};
				list.Add(item2);
			}
		}
		return list;
	}

	private object CreateNextEventAction(string callingEvent)
	{
		return new
		{
			type = "nextEvent",
			value = callingEvent
		};
	}

	private Queue<BAQWhereItem> GetTrackerWhereClause(DashboardViewArgs dashboardViewArgs, PanelCardGridArgs panelCardGridArgs)
	{
		Queue<BAQWhereItem> queue = new Queue<BAQWhereItem>();
		foreach (DashboardViewColumns dashboardViewColumn in dashboardViewArgs.DashboardViewColumns)
		{
			if (dashboardViewColumn != null && dashboardViewColumn.DashboardPrompt && dashboardViewColumn.IsVisible)
			{
				bool honorNull = false;
				if (dashboardViewColumn.DashboardCondition == "Equals" || dashboardViewColumn.DashboardCondition == "NotEqual")
				{
					honorNull = dashboardViewColumn.DashboardHonorNull;
				}
				string columnName = dashboardViewColumn.ColumnName;
				GetConditionAsText(dashboardViewColumn.DashboardCondition);
				string value = "{" + dashboardHelper.GetDataViewName(dashboardDataSet, dashboardViewArgs.DashboardQuery, dashboardViewArgs.DashboardView) + "." + columnName.Trim() + "}";
				string tableName = string.Empty;
				string columnName2 = string.Empty;
				string dataType = string.Empty;
				QueryField queryField = dashboardHelper.GetQueryField(columnName, panelCardGridArgs.DynamicQueryDefinition);
				if (queryField != null)
				{
					tableName = queryField.TableID;
					columnName2 = queryField.FieldName;
					dataType = queryField.DataType;
				}
				if (columnName.Length > 0)
				{
					string conditionAsText = GetConditionAsText(dashboardViewColumn.DashboardCondition);
					queue.Enqueue(baqWhereItemHelper.GetBAQWhereItem(columnName2, tableName, conditionAsText, value, compareColumns: false, isEpBinding: true, dataType, columnName, honorNull));
				}
			}
		}
		return queue;
	}

	private string GetConditionAsText(string dashboardCondition)
	{
		string result = "";
		switch (dashboardCondition)
		{
		case "Equals":
			result = "=";
			break;
		case "NotEqual":
			result = "<>";
			break;
		case "GreaterThan":
			result = ">";
			break;
		case "GreaterThanOrEqualTo":
			result = ">=";
			break;
		case "LessThan":
			result = "<";
			break;
		case "LessThanOrEqualTo":
			result = "<=";
			break;
		case "StartsWith":
			result = "BEGINS";
			break;
		case "Matches":
			result = "MATCHES";
			break;
		}
		return result;
	}

	private Event CreateEventForUpdateAction(PanelCardGridArgs panelCardGridArgs, string actionId)
	{
		Event obj = new Event();
		obj.Name = dashboardHelper.GetCustomActionButtonName(panelCardGridArgs.DashboardViewArgsList[0], actionId);
		obj.Trigger = new Trigger();
		obj.Trigger.Type = "Control";
		obj.Trigger.Target = dashboardHelper.GetCustomActionButtonName(panelCardGridArgs.DashboardViewArgsList[0], actionId);
		obj.Trigger.Hook = "onClick";
		List<object> list = new List<object>();
		foreach (DashboardViewArgs dashboardViewArgs in panelCardGridArgs.DashboardViewArgsList)
		{
			if (dashboardViewArgs.IsUpdatable)
			{
				var anon = new
				{
					type = "erpRest",
					param = new
					{
						requestMethod = "POST",
						erpRequestMethod = "UpdateBaq",
						erpBaqArgs = new
						{
							baqId = dashboardViewArgs.DashboardQuery.DynamicQueryID,
							options = new
							{
								populateDataView = true,
								updateOptions = new
								{
									operation = "UpdateBaq",
									viewName = dashboardViewArgs.DataViewName
								}
							}
						}
					}
				};
				var item = new
				{
					value = "'" + dashboardViewArgs.DataViewName + "'",
					actions = new object[1] { anon }
				};
				list.Add(item);
			}
		}
		var action = new
		{
			type = "switch",
			value = "'%currentDataView%'",
			param = new
			{
				cases = list
			}
		};
		obj.Action = action;
		return obj;
	}

	private Event CreateEventForCustomAction(DashboardViewArgs dashboardViewArgs, string actionId)
	{
		Event obj = new Event();
		obj.Name = dashboardHelper.GetCustomActionButtonName(dashboardViewArgs, actionId);
		obj.Trigger = new Trigger();
		obj.Trigger.Type = "Control";
		obj.Trigger.Target = dashboardHelper.GetCustomActionButtonName(dashboardViewArgs, actionId);
		obj.Trigger.Hook = "onClick";
		var action = new
		{
			type = "erpRest",
			param = new
			{
				requestMethod = "POST",
				erpRequestMethod = "UpdateBaqCustomAction",
				erpBaqArgs = new
				{
					baqId = dashboardViewArgs.DashboardQuery.DynamicQueryID,
					options = new
					{
						populateDataView = true,
						updateOptions = new
						{
							operation = "UpdateBaqCustomAction",
							actionID = actionId,
							viewName = dashboardViewArgs.DataViewName
						}
					}
				}
			}
		};
		obj.Action = action;
		return obj;
	}

	private Event CreateEventForAddAction(DataViewSchema dataViewSchema, DashboardDataSet dashboardDataSet, PanelCardGridArgs panelCardGridArgs, string actionId)
	{
		Event obj = new Event();
		obj.Name = dashboardHelper.GetCustomActionButtonName(panelCardGridArgs.DashboardViewArgsList[0], actionId);
		Trigger trigger = new Trigger();
		trigger.Hook = "onClick";
		trigger.Type = "Control";
		trigger.Target = dashboardHelper.GetCustomActionButtonName(panelCardGridArgs.DashboardViewArgsList[0], actionId);
		obj.Trigger = trigger;
		List<object> list = new List<object>();
		foreach (DashboardViewArgs dashboardViewArgs in panelCardGridArgs.DashboardViewArgsList)
		{
			if (!dashboardViewArgs.AllowAddNew)
			{
				continue;
			}
			var first = dashboardViewArgs.Subscribers.Select((AddNewSubscriber s) => new
			{
				epBinding = dashboardViewArgs.DataViewName + "." + s.AddNewSubColumn,
				value = "{" + getViewName(s.QueryID) + "." + s.SelectedPubColumn + "}"
			});
			var second = from clause in dashboardDataSet.DashboardQueryWhereClause ?? Array.Empty<DashboardQueryWhereClause>()
				where clause.IsPublisher
				let tuple = clause.Value.Split(new char[1] { ':' })
				where tuple.Length > 1
				let publisher = tuple[0]
				let column = tuple[1]
				let publish = dashboardDataSet.DashboardQueryPublishers.FirstOrDefault((DashboardQueryPublishers p) => p.PublisherName.IsEqual(publisher) && p.PublishedColumn.IsEqual(column))
				where publish != null
				select new
				{
					epBinding = getViewName(clause.QueryID) + "." + clause.ColumnName,
					value = "{" + getViewName(publish.QueryID) + "." + publish.PublishedColumn + "}"
				};
			var anon = new
			{
				type = "erpRest",
				param = new
				{
					requestMethod = "POST",
					erpRequestMethod = "UpdateBaqGetNew",
					erpBaqArgs = new
					{
						baqId = dashboardViewArgs.DashboardQuery.DynamicQueryID,
						options = new
						{
							populateDataView = true,
							updateOptions = new
							{
								operation = "UpdateBaqGetNew",
								viewName = dashboardViewArgs.DataViewName
							}
						}
					}
				},
				onSuccess = new object[1]
				{
					new
					{
						type = "row-update",
						param = new object[1]
						{
							new
							{
								columns = first.Union(second).ToArray()
							}
						}
					}
				}
			};
			var item = new
			{
				value = "'" + dashboardViewArgs.DataViewName + "'",
				actions = new object[1] { anon }
			};
			list.Add(item);
		}
		var action = new
		{
			type = "switch",
			value = "'%currentDataView%'",
			param = new
			{
				cases = list
			}
		};
		obj.Action = action;
		return obj;
		string getViewName(string queryId)
		{
			DashboardQuery query = dashboardDataSet.DashboardQuery.FirstOrDefault((DashboardQuery p) => p.QueryID.IsEqual(queryId));
			return dataViewSchema.DataViews.FirstOrDefault((DataView p) => p.Data.IsEqual(query.DynamicQueryID))?.Table;
		}
	}
}
