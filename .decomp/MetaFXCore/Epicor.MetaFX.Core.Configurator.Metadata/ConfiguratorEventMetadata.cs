using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.Configurator.Generation.Utils;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Configurator;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Metadata;

internal class ConfiguratorEventMetadata : EventMetadata
{
	private readonly UpdateConfiguratorResponse updateResponse;

	private readonly IConfiguratorService configurationService;

	private DynamicListHelper dynamicListHelper;

	private const string AllDynamicListEvents = "AllDynamicListEvents";

	private const string DynamicListExpression = "DynamicListExpression";

	private const string DynamicAttribute = "DynamicAttribute";

	private const string TypeCodeValidate = "Validate";

	public ConfiguratorEventMetadata(string rootPath, ILogger logger, Services services, IContentLoader contentLoader, UpdateConfiguratorResponse updateResponse, IConfiguratorService configuratorService, ICacheService cacheService)
		: base(rootPath, logger, services, contentLoader, cacheService)
	{
		this.updateResponse = updateResponse;
		configurationService = configuratorService;
	}

	public override void Save(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		UpdateRequest(request, currentCultureName, localizedStringsFunc);
		base.Save(request, currentCultureName, localizedStringsFunc);
	}

	public JToken GetEvent(EpMetaFxSaveRequestEx request, string eventId, Func<string> currentCultureName)
	{
		JArray allEvents = LoadFileContent(request.id, "events", currentCultureName, isDraft: true)["events"] as JArray;
		return GetEventInternal(allEvents, request, eventId);
	}

	private void UpdateRequest(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		JArray allEvents = LoadFileContent(request.id, "events", currentCultureName, isDraft: true)["events"] as JArray;
		RemoveDynamicListEvents(request, allEvents);
		RemoveDynamicAttributeEventsForRadio(request, allEvents);
		dynamicListHelper = new DynamicListHelper(updateResponse.Configuration, new ConfiguratorGeneratorHelper(), configurationService, logger);
		request.events = dynamicListHelper.UpdateDynamicListsEvents(request.events);
		if (updateResponse.DynamicAttrClass != null)
		{
			request.events = dynamicListHelper.UpdateDynamicAttributeEvents(request.events, updateResponse.DynamicAttrClass);
		}
		AddEventsForNewControls(request, allEvents);
		RemoveEventsForDeletedControls(request, allEvents);
		AddColumnChangingEvent(request, allEvents);
	}

	private void RemoveEventsForDeletedControls(EpMetaFxSaveRequestEx request, JArray allEvents)
	{
		foreach (InputEntity item in updateResponse.InputEntities.Where((InputEntity ip) => ip.Mode == Mode.Removed))
		{
			foreach (string item2 in new List<string>
			{
				item.InputName + "_ColumnChanged",
				item.InputName + "_ColumnChanging",
				"ReadOnly_" + item.InputName
			})
			{
				JToken eventInternal = GetEventInternal(allEvents, request, item2);
				if (eventInternal != null && eventInternal["mode"]?.ToString() != "D")
				{
					if (request.events == null)
					{
						request.events = new JArray();
					}
					request.events.Add(CreateEvent(item2, "D"));
				}
			}
		}
	}

	private void AddColumnChangingEvent(EpMetaFxSaveRequestEx request, JArray allEvents)
	{
		if (allEvents == null)
		{
			return;
		}
		foreach (PcExpr item in updateResponse.Configuration.PcInputsExpr.Where((PcExpr expr) => expr.TypeCode == "Validate" && !string.IsNullOrEmpty(expr.Expression)))
		{
			if (!updateResponse.PreviuosConfiguration.PcInputsExpr.Any((PcExpr expr) => expr.TypeCode == "Validate" && !string.IsNullOrEmpty(expr.Expression) && expr.InputName == item.InputName))
			{
				AddEvent(request, allEvents, item.InputName, "ColumnChanging", (string input) => dynamicListHelper.CreateInputChangingEvent(input));
			}
		}
	}

	private void RemoveDynamicListEvents(EpMetaFxSaveRequestEx request, JArray allEvents)
	{
		List<string> list = new List<string>();
		list.Add("AllDynamicListEvents");
		foreach (PcDynLst item in updateResponse.PreviuosConfiguration.PcDynLst)
		{
			if (!list.Contains("AllDynamicListEvents_" + item.InputName))
			{
				list.Add("AllDynamicListEvents_" + item.InputName);
			}
			if (!list.Contains(string.Format("{0}_{1}_{2}", "DynamicListExpression", item.InputName, item.ListSeq)))
			{
				list.Add(string.Format("{0}_{1}_{2}", "DynamicListExpression", item.InputName, item.ListSeq));
			}
			if (!list.Contains(string.Format("{0}_{1}_Set_{2}", "DynamicListExpression", item.InputName, item.ListSeq)))
			{
				list.Add(string.Format("{0}_{1}_Set_{2}", "DynamicListExpression", item.InputName, item.ListSeq));
			}
			if (!list.Contains("DynamicListExpression_" + item.InputName + "_Default"))
			{
				list.Add("DynamicListExpression_" + item.InputName + "_Default");
			}
		}
		foreach (string item2 in list)
		{
			DeleteEventRequest(request, allEvents, item2);
		}
	}

	private void RemoveDynamicAttributeEventsForRadio(EpMetaFxSaveRequestEx request, JArray allEvents)
	{
		List<string> list = new List<string>();
		foreach (PcInput item in updateResponse.PreviuosConfiguration.PcInputs.Where((PcInput i) => i.ControlType == "Erp.Lib.Configurator.PcControls.EpiRadioSet, Erp.Lib.Configurator" && !string.IsNullOrEmpty(i.AttributeId)))
		{
			if (!list.Contains("DynamicAttribute_" + item.InputName))
			{
				list.Add("DynamicAttribute_" + item.InputName);
			}
		}
		foreach (string item2 in list)
		{
			DeleteEventRequest(request, allEvents, item2);
		}
	}

	private void DeleteEventRequest(EpMetaFxSaveRequestEx request, JArray allEvents, string evt)
	{
		JToken eventInternal = GetEventInternal(allEvents, request, evt);
		if (eventInternal != null && eventInternal["mode"]?.ToString() != "D")
		{
			if (request.events == null)
			{
				request.events = new JArray();
			}
			request.events.Add(CreateEvent(evt, "D"));
		}
	}

	private void AddEventsForNewControls(EpMetaFxSaveRequestEx request, JArray allEvents)
	{
		foreach (InputEntity item in updateResponse.InputEntities.Where((InputEntity ip) => ip.Mode == Mode.Added))
		{
			AddEvent(request, allEvents, item.InputName, "ColumnChanged", (string input) => dynamicListHelper.CreateInputChangeEvent(input));
			AddEvent(request, allEvents, item.InputName, "OnInit", (string input) => dynamicListHelper.CreateInputOnInitEvent(input));
		}
	}

	private void AddEvent(EpMetaFxSaveRequestEx request, JArray allEvents, string inputName, string eventId, Func<string, JArray> func)
	{
		if (GetEventInternal(allEvents, request, inputName + "_" + eventId) != null)
		{
			return;
		}
		JArray jArray = func(inputName);
		if (jArray == null)
		{
			return;
		}
		if (request.events == null)
		{
			request.events = new JArray();
		}
		foreach (JToken item in jArray)
		{
			request.events.Add(item);
		}
	}

	private JToken CreateEvent(string eventId, string mode)
	{
		return JToken.FromObject(new
		{
			id = eventId,
			mode = mode
		});
	}

	private JToken GetEventInternal(JArray allEvents, EpMetaFxSaveRequestEx request, string eventId)
	{
		JToken jToken = null;
		if (request.events != null)
		{
			jToken = request.events.FirstOrDefault((JToken e) => e["id"]?.ToString() == eventId);
		}
		if (jToken == null)
		{
			jToken = allEvents.FirstOrDefault((JToken e) => e["id"]?.ToString() == eventId);
		}
		return jToken;
	}
}
