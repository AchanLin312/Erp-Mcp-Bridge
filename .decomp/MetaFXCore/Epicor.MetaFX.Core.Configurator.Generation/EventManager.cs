using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Configurator.Generation.Utils;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Generation;

internal class EventManager
{
	private readonly ConfiguratorArgs configuratorArgs;

	private readonly DynamicListHelper dynamicListHelper;

	public EventManager(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
		dynamicListHelper = new DynamicListHelper(configuratorArgs.Configuration, configuratorArgs.ConfiguratorHelper, configuratorArgs.ConfiguratorService, configuratorArgs.Logger);
	}

	public List<Event> GenerateInitialEvents()
	{
		List<Event> list = new List<Event>();
		Event obj = CreateWindowOnLoadEvent();
		if (obj != null)
		{
			list.Add(obj);
		}
		return list;
	}

	private Event CreateWindowOnLoadEvent()
	{
		Event obj = new Event
		{
			Name = "window",
			Trigger = new Trigger
			{
				Hook = "onLoad",
				Type = "Window"
			}
		};
		List<object> actions = new List<object> { configuratorArgs.ConfiguratorHelper.CreateNextEventAction("Initialize") };
		obj.Action = configuratorArgs.ConfiguratorHelper.ConvertToAction(actions);
		return obj;
	}

	public EventSchema GenerateSchema()
	{
		EventSchema eventSchema = new EventSchema();
		eventSchema.Schema = "https://schemas.developer.epicor.com/schemas/v2.0/events.json";
		eventSchema.ContentVersion = "2.0";
		eventSchema.Imports = new object[1]
		{
			new
			{
				events = new Reference
				{
					Refs = configuratorArgs.ImportPath + "/events.jsonc#/events"
				}
			}
		};
		return eventSchema;
	}

	public void CreateFile(EventSchema eventSchema)
	{
		JObject data = configuratorArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events");
		configuratorArgs.HelperMethods.CreateJsonFile(data, configuratorArgs.OutputPath, "events.jsonc");
	}

	public List<Event> GenerateEvents()
	{
		List<Event> list = new List<Event>();
		list.Add(CreateExecuteBasicEventsAfterInitialize());
		list.AddRange(GenerateConfiguratorEvents());
		list.AddRange(GeneratePCPageEvents());
		list.AddRange(GeneratePCInputsExprEventsForColumnChanging());
		list.AddRange(GenerateReadOnlyPCPageExprEvents());
		list.AddRange(GenerateReadOnlyPCInputsExprEvents());
		list.AddRange(GeneratePCInputsEventsForColumnChanged());
		list.AddRange(GeneratePCInputsEventsForOnInit());
		list.AddRange(dynamicListHelper.GetDynamicListsEvents());
		if (configuratorArgs.DynamicAttrClass != null)
		{
			list.AddRange(dynamicListHelper.GetDynamicAttibutesEvents(configuratorArgs.DynamicAttrClass));
		}
		return list;
	}

	private List<Event> GeneratePCPageEvents()
	{
		List<Event> list = new List<Event>();
		List<string> list2 = new List<string> { "Leave", "Loaded" };
		foreach (PcPage page in configuratorArgs.Configuration.PcPage)
		{
			foreach (string pagesEvent in list2)
			{
				PcExpr pcExpr = configuratorArgs.Configuration.PcPageExpr.FirstOrDefault((PcExpr exp) => exp.TypeCode == pagesEvent && exp.PageSeq == page.PageSeq && !string.IsNullOrEmpty(exp.Expression));
				if (pcExpr != null)
				{
					Event obj = new Event();
					obj.Name = configuratorArgs.ConfiguratorHelper.GetPageEventName(page.PageSeq, pagesEvent);
					list.Add(obj);
					obj.Action = new
					{
						type = "execute-csharp",
						expression = pcExpr.Expression,
						param = new
						{
							method = configuratorArgs.ConfiguratorHelper.GetDataview(pcExpr.PageSeq.Value) + "_" + pagesEvent,
							type = pagesEvent,
							control = configuratorArgs.ConfiguratorHelper.GetDataview(pcExpr.PageSeq.Value)
						}
					};
				}
			}
		}
		return list;
	}

	private Event CreateExecuteBasicEventsAfterInitialize()
	{
		Event obj = new Event
		{
			Name = "ExecuteBasicEventsAfterInitialize"
		};
		List<object> list = new List<object>();
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction(configuratorArgs.ConfiguratorHelper.GetConfiguratorEventName("OnLoaded")));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("InvokePostExecuteConfiguratorLoadedEvents"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("IterateToSetDefaultVauleForComboBoxes"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("ExecutePageOnLoadEvents"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("InvokeAllPageAndInputsReadonlyEvents"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("InvokeAllInitialImageInputsEvents"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("SetComponentsFromPcFieldPropertiesTransport", new { configuratorArgs.LayoutPage.PageSeq }));
		list.Insert(0, configuratorArgs.ConfiguratorHelper.CreateNextEventAction("DisableColumnEventsForAllPages"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("EnableColumnEventsForAllPages"));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateConditionAction("{PcPage.SkipOnLeaveOPL} === true", new List<object> { configuratorArgs.ConfiguratorHelper.CreateNextEventAction("DisableColumnEventsForAllPages") }));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateNextEventAction("InvokePageLoadedEventIfExists", new { configuratorArgs.LayoutPage.PageSeq }));
		list.Add(configuratorArgs.ConfiguratorHelper.CreateConditionAction("{PcPage.SkipOnLeaveOPL} === true", new List<object> { configuratorArgs.ConfiguratorHelper.CreateNextEventAction("EnableColumnEventsForAllPages") }));
		obj.Action = configuratorArgs.ConfiguratorHelper.ConvertToAction(list);
		return obj;
	}

	private List<Event> GenerateConfiguratorEvents()
	{
		List<Event> list = new List<Event>();
		foreach (string cfgEvent in new List<string> { "OnComplete", "OnLoaded" })
		{
			PcStatusExpr pcStatusExpr = configuratorArgs.Configuration.PcStatusExpr.FirstOrDefault((PcStatusExpr exp) => exp.TypeCode == cfgEvent && !string.IsNullOrEmpty(exp.Expression));
			if (pcStatusExpr != null)
			{
				Event obj = new Event();
				obj.Name = configuratorArgs.ConfiguratorHelper.GetConfiguratorEventName(cfgEvent);
				list.Add(obj);
				obj.Action = new
				{
					type = "execute-csharp",
					expression = pcStatusExpr.Expression,
					param = new
					{
						method = cfgEvent,
						type = cfgEvent,
						control = "Configurator"
					}
				};
			}
		}
		return list;
	}

	private List<Event> GeneratePCInputsEventsForColumnChanged()
	{
		List<Event> list = new List<Event>();
		foreach (PcInput control in configuratorArgs.Configuration.PcInputs.Where((PcInput inp) => !ConfiguratorGeneratorHelper.CONTROLTYPES_WITH_NO_EVENTS.Contains(inp.ControlType)))
		{
			PcExpr pcExpr = configuratorArgs.Configuration.PcInputsExpr.FirstOrDefault((PcExpr i) => !string.IsNullOrEmpty(i.Expression) && i.TypeCode == "Changed" && i.InputName == control.InputName);
			list.AddRange(dynamicListHelper.GeneratePCInputsEventsForColumnChanged(control.InputName, pcExpr?.Expression ?? string.Empty));
		}
		return list;
	}

	private List<Event> GeneratePCInputsEventsForOnInit()
	{
		List<Event> list = new List<Event>();
		foreach (PcInput item in configuratorArgs.Configuration.PcInputs.Where((PcInput inp) => !ConfiguratorGeneratorHelper.CONTROLTYPES_WITH_NO_ONINIT_EVENTS.Contains(inp.ControlType)))
		{
			list.Add(dynamicListHelper.GeneratePCInputsEventsForOnInit(item.InputName));
		}
		return list;
	}

	private List<Event> GeneratePCInputsExprEventsForColumnChanging()
	{
		List<Event> list = new List<Event>();
		foreach (PcExpr item in configuratorArgs.Configuration.PcInputsExpr.Where((PcExpr i) => !string.IsNullOrEmpty(i.Expression) && i.TypeCode == "Validate"))
		{
			list.AddRange(dynamicListHelper.GeneratePCInputsEventsForColumnChanging(item.InputName));
			Event obj = new Event();
			obj.Name = item.InputName + "_ExecuteCsharp_ColumnChanging";
			obj.Action = new
			{
				type = "execute-csharp",
				expression = item.Expression,
				param = new
				{
					method = item.InputName + "_" + item.TypeCode,
					type = item.TypeCode,
					control = item.InputName
				}
			};
			list.Add(obj);
		}
		return list;
	}

	private void SetTrigger(Trigger trigger, string type, string hook, string target)
	{
		trigger.Type = type;
		trigger.Hook = hook;
		trigger.Target = target;
	}

	private List<Event> GenerateReadOnlyPCInputsExprEvents()
	{
		List<Event> list = new List<Event>();
		foreach (PcExpr item in configuratorArgs.Configuration.PcInputsExpr.Where((PcExpr i) => !string.IsNullOrEmpty(i.Expression) && i.TypeCode == "ReadOnlyExpression"))
		{
			Event obj = new Event();
			obj.Name = configuratorArgs.ConfiguratorHelper.GetReadOnlyExpressionEventName(item.InputName);
			obj.Action = new
			{
				type = "execute-csharp",
				expression = item.Expression,
				param = new
				{
					method = item.InputName + "_" + item.TypeCode,
					type = "ReadOnlyExpression",
					control = item.InputName
				}
			};
			list.Add(obj);
		}
		return list;
	}

	private List<Event> GenerateReadOnlyPCPageExprEvents()
	{
		List<Event> list = new List<Event>();
		foreach (PcExpr item in configuratorArgs.Configuration.PcPageExpr.Where((PcExpr i) => !string.IsNullOrEmpty(i.Expression) && i.TypeCode == "ReadOnlyExpression"))
		{
			Event obj = new Event();
			obj.Name = configuratorArgs.ConfiguratorHelper.GetReadOnlyExpressionEventName(configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq.Value));
			obj.Action = new
			{
				type = "execute-csharp",
				expression = item.Expression,
				param = new
				{
					method = configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq.Value) + "_" + item.TypeCode,
					type = "Page_ReadOnlyExpression",
					control = configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq.Value)
				}
			};
			list.Add(obj);
		}
		return list;
	}

	private object CreateDisableColumnChangeAction(string dataTable)
	{
		return new
		{
			type = "disableColumnChangeEvents",
			dataTable = dataTable
		};
	}

	private object CreateEnableColumnChangeAction(string dataTable)
	{
		return new
		{
			type = "enableColumnChangeEvents",
			dataTable = dataTable
		};
	}
}
