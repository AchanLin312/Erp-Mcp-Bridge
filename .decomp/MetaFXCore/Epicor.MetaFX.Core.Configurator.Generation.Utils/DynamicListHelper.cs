using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Epicor.MetaFX.Core.Models.MetaUI.Event;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Epicor.MetaFX.Core.Configurator.Generation.Utils;

public class DynamicListHelper
{
	private readonly Configuration configuration;

	private readonly ConfiguratorGeneratorHelper configuratorHelper;

	private readonly IConfiguratorService configuratorService;

	private readonly ILogger Logger;

	private readonly JsonSerializer serializer;

	public DynamicListHelper(Configuration configuration, ConfiguratorGeneratorHelper configuratorHelper, IConfiguratorService configuratorService, ILogger Logger)
	{
		this.configuration = configuration;
		this.configuratorHelper = configuratorHelper;
		this.configuratorService = configuratorService;
		this.Logger = Logger;
		serializer = new JsonSerializer
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			NullValueHandling = NullValueHandling.Ignore
		};
	}

	public EventSchema GenerateSchema()
	{
		EventSchema eventSchema = new EventSchema();
		eventSchema.Schema = "https://schemas.developer.epicor.com/schemas/v2.0/events.json";
		eventSchema.ContentVersion = "2.0";
		eventSchema.Imports = new object[0];
		return eventSchema;
	}

	public JArray UpdateDynamicListsEvents(JArray modifedEvents)
	{
		if (modifedEvents == null)
		{
			modifedEvents = new JArray();
		}
		List<Event> dynamicListsEvents = GetDynamicListsEvents();
		EventSchema eventSchema = GenerateSchema();
		eventSchema.Events.AddRange(dynamicListsEvents);
		foreach (JToken item in new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(Logger).UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events")["events"] as JArray)
		{
			item["mode"] = "A";
			modifedEvents.Add(item);
		}
		return modifedEvents;
	}

	public JArray UpdateDynamicAttributeEvents(JArray modifedEvents, DynamicAttrClass dynamicAttrClass)
	{
		if (modifedEvents == null)
		{
			modifedEvents = new JArray();
		}
		List<Event> dynamicAttibutesEvents = GetDynamicAttibutesEvents(dynamicAttrClass);
		EventSchema eventSchema = GenerateSchema();
		eventSchema.Events.AddRange(dynamicAttibutesEvents);
		foreach (JToken item in new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(Logger).UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events")["events"] as JArray)
		{
			item["mode"] = "A";
			modifedEvents.Add(item);
		}
		return modifedEvents;
	}

	public JArray CreateInputChangeEvent(string inputName)
	{
		List<Event> list = GeneratePCInputsEventsForColumnChanged(inputName);
		if (list == null)
		{
			return null;
		}
		EventSchema eventSchema = GenerateSchema();
		eventSchema.Events.AddRange(list);
		JObject jObject = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(Logger).UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events");
		foreach (JToken item in jObject["events"] as JArray)
		{
			item["mode"] = "A";
		}
		return jObject["events"] as JArray;
	}

	public JArray CreateInputChangingEvent(string inputName)
	{
		List<Event> list = GeneratePCInputsEventsForColumnChanging(inputName);
		if (list == null)
		{
			return null;
		}
		EventSchema eventSchema = GenerateSchema();
		eventSchema.Events.AddRange(list);
		JObject jObject = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(Logger).UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events");
		foreach (JToken item in jObject["events"] as JArray)
		{
			item["mode"] = "A";
		}
		return jObject["events"] as JArray;
	}

	public JArray CreateInputOnInitEvent(string inputName)
	{
		Event obj = GeneratePCInputsEventsForOnInit(inputName);
		if (obj == null)
		{
			return null;
		}
		EventSchema eventSchema = GenerateSchema();
		eventSchema.Events.Add(obj);
		JToken jToken = new Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade(Logger).UpgradeToLatestSchema(JObject.FromObject(eventSchema), "events")["events"][0];
		jToken["mode"] = "A";
		return new JArray { jToken };
	}

	public List<Event> GetDynamicListsEvents()
	{
		List<Event> list = new List<Event>();
		list.Add(GenerateAllDynamicListExprEvents());
		list.AddRange(GenerateAllDynamicListExprEventsForInputs());
		list.AddRange(GenerateDynamicListExprEvents());
		list.AddRange(GenerateDynamicListSetPropertyEvents());
		list.AddRange(GenerateDynamicListUDMethodSetPropertyEvents());
		return list;
	}

	private Event GenerateAllDynamicListExprEvents()
	{
		Event obj = new Event();
		obj.Name = "AllDynamicListEvents";
		List<object> list = new List<object>();
		foreach (PcDynLst item in configuration.PcDynLst.Where((PcDynLst i) => !string.IsNullOrEmpty(i.Condition)))
		{
			list.Add(configuratorHelper.CreateNextEventAction(configuratorHelper.GetDynamicListExpressionEventName(item.InputName, Convert.ToString(item.ListSeq))));
		}
		obj.Action = configuratorHelper.ConvertToAction(list);
		return obj;
	}

	private List<Event> GenerateAllDynamicListExprEventsForInputs()
	{
		List<Event> list = new List<Event>();
		foreach (PcInput input in configuration.PcInputs)
		{
			Event obj = new Event();
			obj.Name = "AllDynamicListEvents_" + input.InputName;
			List<object> list2 = new List<object>();
			string text = string.Empty;
			foreach (PcDynLst item in from x in configuration.PcDynLst
				where !string.IsNullOrEmpty(x.Condition) && x.InputName == input.InputName
				orderby x.ListSeq
				select x)
			{
				if (list2.Count <= 0)
				{
					list2.Add(configuratorHelper.CreateNextEventAction(configuratorHelper.GetDynamicListExpressionEventName(item.InputName, Convert.ToString(item.ListSeq))));
					text = string.Format("'{{{0}.{1}_DynLst_{2}}}' === 'false'", "TransView", item.InputName, item.ListSeq);
				}
				else
				{
					list2.Add(new
					{
						type = "condition",
						param = new
						{
							expression = text,
							onSuccess = new
							{
								actions = new object[1] { configuratorHelper.CreateNextEventAction(configuratorHelper.GetDynamicListExpressionEventName(item.InputName, Convert.ToString(item.ListSeq))) }
							}
						}
					});
					text += string.Format(" && '{{{0}.{1}_DynLst_{2}}}' === 'false'", "TransView", item.InputName, item.ListSeq);
				}
			}
			obj.Action = configuratorHelper.ConvertToAction(list2);
			if (list2.Any())
			{
				Event obj2 = GenerateDefaultDynamicListEvent(input);
				list.Add(obj2);
				list2.Add(configuratorHelper.CreateConditionAction(text, new List<object> { configuratorHelper.CreateNextEventAction(obj2.Name) }));
				list.Add(obj);
			}
		}
		return list;
	}

	private Event GenerateDefaultDynamicListEvent(PcInput input)
	{
		Event obj = new Event
		{
			Name = configuratorHelper.GetDynamicListExpressionEventName(input.InputName, "Default")
		};
		List<object> list = new List<object>();
		List<object> commonProps = configuratorHelper.GetCommonProps("List", string.Empty, string.Empty, "false", "true", input.InitVal, GetComboFields(input.ListItems));
		commonProps.Add(new
		{
			prop = "returnObj",
			value = string.Empty
		});
		list.Add(new
		{
			type = "setProperty",
			param = new
			{
				component = configuratorHelper.GetDataview(input.PageSeq) + "." + input.InputName,
				props = commonProps
			}
		});
		object item = configuratorHelper.CreateUpdateRowAction(configuratorHelper.GetDataview(input.PageSeq) + "." + input.InputName, input.InitVal);
		list.Add(configuratorHelper.CreateConditionAction(CreateEmptyCheckExpression(configuratorHelper.GetDataview(input.PageSeq) + "." + input.InputName), new List<object> { item }));
		obj.Action = configuratorHelper.ConvertToAction(list);
		return obj;
	}

	private List<Event> GenerateDynamicListExprEvents()
	{
		List<Event> list = new List<Event>();
		foreach (PcDynLst item in configuration.PcDynLst.Where((PcDynLst i) => !string.IsNullOrEmpty(i.Condition)))
		{
			Event obj = new Event();
			obj.Name = configuratorHelper.GetDynamicListExpressionEventName(item.InputName, Convert.ToString(item.ListSeq));
			obj.Action = configuratorHelper.ConvertToAction(GetDynamicListActions(item));
			list.Add(obj);
		}
		return list;
	}

	private List<object> GetDynamicListActions(PcDynLst item)
	{
		List<object> list = new List<object>();
		list.Add(new
		{
			type = "execute-csharp",
			expression = item.Condition,
			param = new
			{
				method = $"{item.InputName}_DynLst_{item.ListSeq}",
				type = "DynamicListExpression",
				control = item.InputName,
				outputs = new[]
				{
					new
					{
						name = $"{item.InputName}_DynLst_{item.ListSeq}",
						value = string.Format("{0}.{1}_DynLst_{2}", "TransView", item.InputName, item.ListSeq)
					}
				}
			}
		});
		list.Add(new
		{
			type = "condition",
			param = new
			{
				expression = string.Format("'{{{0}.{1}_DynLst_{2}}}' === 'true'", "TransView", item.InputName, item.ListSeq),
				onSuccess = new
				{
					actions = new[]
					{
						new
						{
							type = "nextEvent",
							value = configuratorHelper.GetDynamicListExpressionSetPropertyName(item.InputName, Convert.ToString(item.ListSeq))
						}
					}
				}
			}
		});
		return list;
	}

	private List<Event> GenerateDynamicListSetPropertyEvents()
	{
		List<Event> list = new List<Event>();
		foreach (PcDynLst dynamicList in configuration.PcDynLst.Where((PcDynLst x) => x.ListDataSource != "UDM"))
		{
			Event obj = new Event();
			List<PcDynLstParam> dynamicListParams = configuration.PcDynLstParams.Where((PcDynLstParam x) => x.InputName == dynamicList.InputName && x.ListSeq == dynamicList.ListSeq).ToList();
			obj.Name = configuratorHelper.GetDynamicListExpressionSetPropertyName(dynamicList.InputName, Convert.ToString(dynamicList.ListSeq));
			obj.Action = configuratorHelper.ConvertToAction(GenerateDynamicListSetPropertyActions(dynamicList, dynamicListParams));
			list.Add(obj);
		}
		return list;
	}

	private List<object> GenerateDynamicListSetPropertyActions(PcDynLst dynamicList, List<PcDynLstParam> dynamicListParams)
	{
		List<object> obj = new List<object>
		{
			new
			{
				type = "setProperty",
				param = new
				{
					component = configuratorHelper.GetDataview(dynamicList.PageSeq) + "." + dynamicList.InputName,
					props = GenerateDynamicListMethodParameters(dynamicList, dynamicListParams)
				}
			}
		};
		object item = configuratorHelper.CreateUpdateRowAction(configuratorHelper.GetDataview(dynamicList.PageSeq) + "." + dynamicList.InputName, dynamicList.InitVal);
		obj.Add(configuratorHelper.CreateConditionAction(CreateEmptyCheckExpression(configuratorHelper.GetDataview(dynamicList.PageSeq) + "." + dynamicList.InputName), new List<object> { item }));
		return obj;
	}

	private List<object> GenerateDynamicListMethodParameters(PcDynLst dynamicList, List<PcDynLstParam> dynamicListParams)
	{
		string listDataSource = dynamicList.ListDataSource;
		if (!(listDataSource == "LooK"))
		{
			if (listDataSource == "BAQ")
			{
				return new BAQHelper(new ConfiguratorArgs
				{
					Configuration = configuration,
					ConfiguratorHelper = configuratorHelper,
					ConfiguratorService = configuratorService
				}).GenerateBAQMethodParameters(dynamicList, dynamicListParams);
			}
			return GenerateListMethodParameters(dynamicList);
		}
		return GenerateLookUpMethodParameters(dynamicList, dynamicListParams);
	}

	private List<object> GenerateLookUpMethodParameters(PcDynLst dynamicList, List<PcDynLstParam> dynamicListParams)
	{
		List<object> commonProps = configuratorHelper.GetCommonProps(dynamicList.ListDataSource, "Erp.BO.ConfigurationRuntimeSvc", GetLookUpFunction(dynamicList.TblLookupFunc), "true", "true", dynamicList.InitVal, new object[0]);
		commonProps.Add(new
		{
			prop = "retrieveOnActivate",
			value = "false"
		});
		List<string> list = new List<string>();
		list.Add("configId = '" + dynamicList.ConfigId + "'");
		list.Add("testId = '00000000-0000-0000-0000-000000000000'");
		if (dynamicList.TblLookupFunc == "DataRowLookup" || dynamicList.TblLookupFunc == "DataColumnLookupList")
		{
			list.Add("lookupTblID = '" + dynamicList.TblLookupId + "'");
		}
		else
		{
			list.Add("lookupTableID = '" + dynamicList.TblLookupId + "'");
		}
		foreach (PcDynLstParam dynamicListParam in dynamicListParams)
		{
			string paramName = dynamicListParam.ParamName;
			if (!(paramName == "endColumn"))
			{
				if (paramName == "startColumn")
				{
					list.Add("startCol = '" + FormatUserDefinedMethodParams(dynamicListParam, "string", dynamicListParam.ParamValue, dynamicListParam.ParamConst) + "'");
				}
				else
				{
					list.Add(dynamicListParam.ParamName + " = '" + FormatUserDefinedMethodParams(dynamicListParam, "string", dynamicListParam.ParamValue, dynamicListParam.ParamConst) + "'");
				}
			}
			else
			{
				list.Add("endCol = '" + FormatUserDefinedMethodParams(dynamicListParam, "string", dynamicListParam.ParamValue, dynamicListParam.ParamConst) + "'");
			}
		}
		commonProps.Add(new
		{
			prop = "restArguments",
			value = list
		});
		return commonProps;
	}

	private List<object> GenerateListMethodParameters(PcDynLst dynamicList)
	{
		List<object> commonProps = configuratorHelper.GetCommonProps(dynamicList.ListDataSource, string.Empty, string.Empty, "false", "true", dynamicList.InitVal, GetComboFields(dynamicList.ListItems));
		commonProps.Add(new
		{
			prop = "returnObj",
			value = string.Empty
		});
		return commonProps;
	}

	private List<Event> GenerateDynamicListUDMethodSetPropertyEvents()
	{
		List<Event> list = new List<Event>();
		ConfiguratorUDFunction pcFunctionDefs = new UDHelper(new ConfiguratorArgs
		{
			Configuration = configuration,
			ConfiguratorHelper = configuratorHelper,
			ConfiguratorService = configuratorService
		}).GetPcFunctionDefs();
		List<PcFunctionDef> pcFunctionDef = pcFunctionDefs.PcFunctionDef;
		foreach (PcDynLst dynamicList in configuration.PcDynLst.Where((PcDynLst x) => x.ListDataSource == "UDM"))
		{
			Event obj = new Event();
			PcFunctionDef pcFunctionDef2 = pcFunctionDef.FirstOrDefault((PcFunctionDef x) => x.FunctionName == dynamicList.UserDefinedName);
			List<PcFunctionParam> pcFunctionParam = (from x in pcFunctionDefs.PcFunctionParam
				where x.FunctionName == dynamicList.UserDefinedName
				orderby x.ParamSeq
				select x).ToList();
			List<PcDynLstParam> dynamicListParams = configuration.PcDynLstParams.Where((PcDynLstParam x) => x.InputName == dynamicList.InputName && x.ListSeq == dynamicList.ListSeq).ToList();
			obj.Name = configuratorHelper.GetDynamicListExpressionSetPropertyName(dynamicList.InputName, Convert.ToString(dynamicList.ListSeq));
			if (dynamicList.UserDefinedFunctionType == "client")
			{
				obj.Action = configuratorHelper.ConvertToAction(new List<object>
				{
					new
					{
						type = "execute-csharp",
						expression = pcFunctionDef2.Expression,
						param = new
						{
							method = dynamicList.UserDefinedName,
							inputs = GenerateDynamicListUDMethodInputs(dynamicListParams, pcFunctionParam),
							type = "DynamicListExpressionUD",
							control = dynamicList.InputName,
							returnType = pcFunctionDef2.ReturnType
						}
					}
				});
				list.Add(obj);
			}
			else
			{
				obj.Action = configuratorHelper.ConvertToAction(new List<object>
				{
					new
					{
						type = "execute-csharp",
						expression = string.Empty,
						param = new
						{
							method = dynamicList.UserDefinedName,
							inputs = GenerateDynamicListUDMethodInputs(dynamicListParams, pcFunctionParam),
							type = "DynamicListExpressionUD",
							control = dynamicList.InputName,
							returnType = pcFunctionDef2.ReturnType
						}
					}
				});
				list.Add(obj);
			}
		}
		return list;
	}

	private List<object> GenerateDynamicListUDMethodInputs(List<PcDynLstParam> dynamicListParams, List<PcFunctionParam> pcFunctionParam)
	{
		List<object> list = new List<object>();
		foreach (PcFunctionParam functionParam in pcFunctionParam)
		{
			PcDynLstParam pcDynLstParam = dynamicListParams.Find((PcDynLstParam x) => x.ParamName == functionParam.ParameterName);
			if (pcDynLstParam == null)
			{
				continue;
			}
			if (pcDynLstParam.ParamType.ToUpperInvariant() != "BOOL")
			{
				list.Add(new
				{
					name = pcDynLstParam.ParamName,
					type = ((!pcDynLstParam.ParamConst) ? "epBinding" : ""),
					value = FormatUserDefinedMethodParams(pcDynLstParam, pcDynLstParam.ParamType, pcDynLstParam.ParamValue, pcDynLstParam.ParamConst, pcDynLstParam.ParamModifier)
				});
				continue;
			}
			string text = FormatUserDefinedMethodParams(pcDynLstParam, pcDynLstParam.ParamType, pcDynLstParam.ParamValue, pcDynLstParam.ParamConst, pcDynLstParam.ParamModifier);
			if (pcDynLstParam.ParamConst && (text == "true" || text == "false"))
			{
				list.Add(new
				{
					name = pcDynLstParam.ParamName,
					type = ((!pcDynLstParam.ParamConst) ? "epBinding" : ""),
					value = Convert.ToBoolean(text)
				});
			}
			else
			{
				list.Add(new
				{
					name = pcDynLstParam.ParamName,
					type = ((!pcDynLstParam.ParamConst) ? "epBinding" : ""),
					value = text
				});
			}
		}
		return list;
	}

	private string GetLookUpFunction(string lookupFunc)
	{
		if (lookupFunc == "DataColRange")
		{
			return "DataColumnRange";
		}
		return lookupFunc;
	}

	private string CreateEmptyCheckExpression(string variableToCheck)
	{
		return "'{" + variableToCheck + "}' === '' || '{" + variableToCheck + "}' === 'null' || '{" + variableToCheck + "}' === 'undefined'";
	}

	private string FormatUserDefinedMethodParams(PcDynLstParam param, string paramType, string paramValue, bool paramConst = true, string paramModifier = "", string varName = "")
	{
		string result = string.Empty;
		switch (paramType.ToUpperInvariant())
		{
		case "STRING":
		case "INT":
		case "BOOL":
		case "DECIMAL":
		case "DATETIME":
			result = ((!paramConst) ? ((string.IsNullOrEmpty(paramModifier) && string.IsNullOrEmpty(varName)) ? $"{{Page{param.PageSeq}.{param.ParamInput}}}" : (paramModifier + " " + varName)) : paramValue);
			break;
		}
		return result;
	}

	private object[] GetComboFields(string comboStr)
	{
		string[] array = comboStr.Split(new char[1] { '~' });
		List<object> list = new List<object>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(new char[1] { '`' });
			if (array3.Length >= 2)
			{
				list.Add(new
				{
					display = array3[1],
					value = array3[0]
				});
			}
		}
		return list.ToArray();
	}

	private void SetTrigger(Trigger trigger, string type, string hook, string target)
	{
		trigger.Type = type;
		trigger.Hook = hook;
		trigger.Target = target;
	}

	public Event GeneratePCInputsEventsForOnInit(string inputName)
	{
		PcInput pcInput = configuration.PcInputs.FirstOrDefault((PcInput i) => i.InputName == inputName);
		if (pcInput == null || Enumerable.Contains(ConfiguratorGeneratorHelper.CONTROLTYPES_WITH_NO_EVENTS, pcInput.ControlType))
		{
			return null;
		}
		Event obj = new Event
		{
			Name = inputName + "_OnInit"
		};
		Trigger trigger = new Trigger();
		SetTrigger(trigger, "EpBinding", "onInit", configuratorHelper.GetDataview(pcInput.PageSeq) + "." + inputName);
		obj.Trigger = trigger;
		List<object> actions = new List<object> { configuratorHelper.CreateNextEventAction("SetComponentFromPcFieldPropertiesTransport", new
		{
			InputName = inputName
		}) };
		obj.Action = configuratorHelper.CreateConditionAction("{TransView.initializingConfigurator} === false", actions);
		return obj;
	}

	public List<Event> GeneratePCInputsEventsForColumnChanging(string inputName)
	{
		List<Event> list = new List<Event>();
		PcInput pcInput = configuration.PcInputs.FirstOrDefault((PcInput input) => input.InputName == inputName);
		if (pcInput == null)
		{
			return null;
		}
		Event obj = new Event();
		Trigger trigger = new Trigger();
		obj.Name = pcInput.InputName + "_ColumnChanging";
		SetTrigger(trigger, "DataTable", "columnChanging", configuratorHelper.GetDataview(pcInput.PageSeq));
		trigger.Columns = new List<string> { pcInput.InputName };
		obj.Trigger = trigger;
		List<object> list2 = new List<object>();
		list2.Add(configuratorHelper.CreateNextEventAction("InitializeColumnChangingSettings"));
		list2.Add(configuratorHelper.CreateNextEventAction(pcInput.InputName + "_ExecuteCsharp_ColumnChanging"));
		list2.Add(configuratorHelper.CreateNextEventAction("ValidateColumnChanging"));
		obj.Action = configuratorHelper.ConvertToAction(list2);
		list.Add(obj);
		return list;
	}

	public List<Event> GeneratePCInputsEventsForColumnChanged(string inputName, string expression = "")
	{
		PcInput pcInput = configuration.PcInputs.FirstOrDefault((PcInput i) => i.InputName == inputName);
		if (pcInput == null || Enumerable.Contains(ConfiguratorGeneratorHelper.CONTROLTYPES_WITH_NO_EVENTS, pcInput.ControlType))
		{
			return null;
		}
		List<Event> list = new List<Event>();
		Event obj = new Event
		{
			Name = inputName + "_ColumnChanged"
		};
		Trigger trigger = new Trigger();
		if (pcInput.ControlType == "Ice.Lib.Framework.EpiButton, Ice.Lib.EpiClientLib")
		{
			SetTrigger(trigger, "EpBinding", "onClick", configuratorHelper.GetDataview(pcInput.PageSeq) + "." + inputName);
			obj.Trigger = trigger;
		}
		else
		{
			SetTrigger(trigger, "DataTable", "columnChanged", configuratorHelper.GetDataview(pcInput.PageSeq));
			trigger.Columns = new List<string> { inputName };
			obj.Trigger = trigger;
		}
		List<object> list2 = new List<object>();
		list2.AddRange(GeneratePreExecuteCSharpChangedEvents(inputName, pcInput.PageSeq));
		list2.Add(configuratorHelper.CreateConditionAction("#_trans.dataView('PcInputsExpr').findRowIndex(r => r.InputName === '" + inputName + "' && r.TypeCode=='Changed' && r.Expression) > -1_#", new List<object> { configuratorHelper.CreateNextEventAction(inputName + "_ExecuteCsharp_ColumnChanged") }));
		list2.Add(configuratorHelper.CreateNextEventAction("InvokePostExecuteCSharpChangedEvents", new
		{
			SelectedColumnChanged = pcInput.InputName
		}));
		obj.Action = configuratorHelper.ConvertToAction(list2);
		list.Add(obj);
		list.Add(new Event
		{
			Name = inputName + "_ExecuteCsharp_ColumnChanged",
			Action = new
			{
				type = "execute-csharp",
				expression = expression,
				param = new
				{
					method = pcInput.InputName + "_Changed",
					type = "Changed",
					control = pcInput.InputName
				}
			}
		});
		return list;
	}

	public List<object> GeneratePreExecuteCSharpChangedEvents(string controlModified, long pageSeq)
	{
		return new List<object> { configuratorHelper.CreateNextEventAction("InvokePreExecuteCSharpChangedEvents", new
		{
			SelectedColumnChanged = controlModified,
			CurrentPage = configuratorHelper.GetDataview(pageSeq)
		}) };
	}

	public List<Event> GetDynamicAttibutesEvents(DynamicAttrClass dynamicAttrClass)
	{
		List<Event> list = new List<Event>();
		foreach (PcInput item in configuration.PcInputs.Where((PcInput i) => i.ControlType == "Erp.Lib.Configurator.PcControls.EpiRadioSet, Erp.Lib.Configurator" && !string.IsNullOrEmpty(i.AttributeId) && i.UseListValues && i.ExternalRef))
		{
			if (configuratorHelper.FindDynAttrClassDtl(dynamicAttrClass, item.Company, "Erp", "Part", configuration.PcStatus[0].AttrClassId, item.AttributeId) != null)
			{
				string comboStr = configuratorHelper.BuildDynAttrListValues(dynamicAttrClass, item.Company, "Erp", "Part", configuration.PcStatus[0].AttrClassId, item.AttributeId);
				Event obj = new Event
				{
					Name = configuratorHelper.GetDynamicAttributeEventName(item.InputName)
				};
				obj.Action = configuratorHelper.ConvertToAction(GenerateDynamicAttributeSetPropertyActions(item, configuratorHelper.GetRadioFields(comboStr)));
				list.Add(obj);
			}
		}
		return list;
	}

	private List<object> GenerateDynamicAttributeSetPropertyActions(PcInput control, DataFields[] dataFields)
	{
		return new List<object>
		{
			new
			{
				type = "setProperty",
				param = new
				{
					component = configuratorHelper.GetDataview(control.PageSeq) + "." + control.InputName,
					props = new List<object>
					{
						new
						{
							prop = "data",
							value = JArray.FromObject(dataFields, serializer)
						}
					}
				}
			}
		};
	}
}
