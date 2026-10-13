using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Events;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Events;

public class EventDesignerManager
{
	private readonly HelperMethods helper;

	private readonly IEventDesignerService eventDesignerService;

	private readonly IContentLoader contentLoader;

	private readonly ILogger logger;

	private readonly string rootPath;

	private const string MODE_PROPERTY = "mode";

	private const string DELETE = "D";

	private const string REVERT = "R";

	private const string ID_PROPERTY = "id";

	public EventDesignerManager(string rootPath, ILogger logger, IEventDesignerService eventDesignerService, IContentLoader contentLoader)
	{
		this.rootPath = rootPath;
		this.logger = logger;
		this.eventDesignerService = eventDesignerService;
		this.contentLoader = contentLoader;
		helper = new HelperMethods(this.logger, contentLoader);
	}

	internal void Update(EpMetaFxSaveRequestEx request)
	{
		if (request.events == null)
		{
			return;
		}
		logger.Debug("Update the event designer for " + request.layerInfo?.layerName + " for the " + request.id + ".");
		JArray events = request.events;
		List<string> list = new List<string>();
		for (int i = 0; i < events.Count; i++)
		{
			if (events[i]["mode"] != null && events[i]["id"] != null && !string.IsNullOrEmpty(events[i]["id"].ToString()) && ("D".Equals(events[i]["mode"].ToString(), StringComparison.OrdinalIgnoreCase) || "R".Equals(events[i]["mode"].ToString(), StringComparison.OrdinalIgnoreCase)))
			{
				list.Add(events[i]["id"].ToString());
			}
		}
		if (list.Count == 0 && (request.serializedEvents == null || request.serializedEvents.Count == 0) && events.Any((JToken x) => x["mode"] == null || !x["mode"].Equals("U")))
		{
			return;
		}
		EventDesignerRequest eventDesignerRequest = new EventDesignerRequest
		{
			ViewId = request.id,
			Company = ((request.layerInfo != null) ? request.layerInfo.company : string.Empty),
			LayerName = ((request.layerInfo != null) ? request.layerInfo.layerName : string.Empty),
			DeviceType = request.deviceType,
			CGCCode = ((request.layerInfo != null) ? request.layerInfo.CGCCode : string.Empty),
			TypeCode = "KNTCEventLayout"
		};
		EventDesigner eventDesigner = eventDesignerService.GetDesigner(eventDesignerRequest);
		if (eventDesigner != null && list.Count > 0)
		{
			foreach (string item in list)
			{
				if (eventDesigner.Content.ContainsKey(item))
				{
					eventDesigner.Content.Remove(item);
				}
			}
		}
		if (request.serializedEvents != null)
		{
			foreach (KeyValuePair<string, JToken> serializedEvent in request.serializedEvents)
			{
				if (!list.Contains(serializedEvent.Key))
				{
					if (eventDesigner == null)
					{
						EventDesigner eventDesigner2 = new EventDesigner();
						eventDesigner2.Company = eventDesignerRequest.Company;
						eventDesigner2.DeviceType = eventDesignerRequest.DeviceType;
						eventDesigner2.LayerName = eventDesignerRequest.LayerName;
						eventDesigner2.ViewId = eventDesignerRequest.ViewId;
						eventDesigner2.CGCCode = eventDesignerRequest.CGCCode;
						eventDesigner2.TypeCode = eventDesignerRequest.TypeCode;
						eventDesigner2.ParentLayers = ((request.layerInfo != null) ? request.layerInfo.parentLayers : new string[0]);
						eventDesigner = eventDesigner2;
					}
					eventDesigner.Content[serializedEvent.Key] = serializedEvent.Value;
				}
			}
		}
		if (eventDesigner != null)
		{
			foreach (JToken @event in request.events)
			{
				string key = @event["id"].ToString();
				eventDesigner.Content[key] = @event;
			}
		}
		if (eventDesigner != null && !eventDesignerService.SaveDesigner(eventDesigner))
		{
			logger.Debug("Unable to update event designer for " + eventDesignerRequest.LayerName + "-" + eventDesignerRequest.ViewId + ".");
			throw new Exception("Unable to update event designer.");
		}
		logger.Debug("Updated the event designer for " + eventDesignerRequest.LayerName + " for the " + eventDesignerRequest.ViewId + ".");
	}

	public void Delete(EventDesignerRequest request)
	{
		logger.Debug("Delete event designer for the layer " + request.LayerName + " for the " + request.ViewId + ".");
		helper.Validate(request);
		List<EventDesigner> designers = eventDesignerService.GetDesigners(new EventDesignersRequest
		{
			TypeCode = request.TypeCode,
			ViewId = request.ViewId,
			Company = request.Company,
			CGCCode = request.CGCCode
		});
		Delete(request, designers);
		logger.Debug("Deleted event designer for the layer " + request.LayerName + " for the " + request.ViewId + ".");
	}

	public EpMetaFxEventDesigner Get(EpMetaFxEventDesignerRequest request)
	{
		helper.Validate(request);
		EventDesignerRequest eventDesignerRequest = new EventDesignerRequest
		{
			Company = string.Empty,
			DeviceType = request.DeviceType,
			LayerName = string.Empty,
			ViewId = request.ViewId,
			CGCCode = string.Empty,
			TypeCode = "KNTCEventLayout"
		};
		if (request.Layers == null || !request.Layers.Any())
		{
			return GetLayoutByLayerName(eventDesignerRequest, request.EventId);
		}
		List<EventDesigner> designers = eventDesignerService.GetDesigners(new EventDesignersRequest
		{
			TypeCode = "KNTCEventLayout",
			ViewId = request.ViewId
		});
		List<EventDesignerRequest> list = new List<EventDesignerRequest>();
		list.Add(eventDesignerRequest);
		foreach (string layer in request.Layers)
		{
			if (!string.IsNullOrEmpty(layer))
			{
				BuildList(layer, designers, list);
			}
		}
		foreach (EventDesignerRequest item in list)
		{
			EpMetaFxEventDesigner layoutByLayerName = GetLayoutByLayerName(item, request.EventId);
			if (layoutByLayerName != null)
			{
				return layoutByLayerName;
			}
		}
		return null;
	}

	private void BuildList(string layerName, List<EventDesigner> designerList, List<EventDesignerRequest> list)
	{
		EventDesigner eventDesigner = designerList.FirstOrDefault((EventDesigner ly) => ly.LayerName == layerName);
		if (eventDesigner == null)
		{
			return;
		}
		EventDesignerRequest item = new EventDesignerRequest
		{
			Company = eventDesigner.Company,
			DeviceType = eventDesigner.DeviceType,
			LayerName = eventDesigner.LayerName,
			ViewId = eventDesigner.ViewId,
			CGCCode = eventDesigner.CGCCode,
			TypeCode = eventDesigner.TypeCode
		};
		list.Insert(0, item);
		if (eventDesigner.ParentLayers != null)
		{
			string[] parentLayers = eventDesigner.ParentLayers;
			foreach (string layerName2 in parentLayers)
			{
				BuildList(layerName2, designerList, list);
			}
		}
	}

	private EpMetaFxEventDesigner GetLayoutByLayerName(EventDesignerRequest request, string eventId)
	{
		EventDesigner designer = eventDesignerService.GetDesigner(request);
		if (designer == null || !designer.Content.ContainsKey(eventId))
		{
			return null;
		}
		return new EpMetaFxEventDesigner
		{
			Company = designer.Company,
			DeviceType = designer.DeviceType,
			LayerName = designer.LayerName,
			ViewId = designer.ViewId,
			EventId = eventId,
			CGCCode = designer.CGCCode,
			Content = designer.Content[eventId]
		};
	}

	private void Delete(EventDesignerRequest request, List<EventDesigner> designerList)
	{
		if (designerList.FirstOrDefault((EventDesigner layout) => layout.LayerName == request.LayerName) == null)
		{
			return;
		}
		eventDesignerService.DeleteDesigner(request);
		logger.Debug("Deleted event designer for the layer " + request.LayerName + " for the " + request.ViewId + ".");
		foreach (EventDesigner item in designerList.FindAll((EventDesigner layout) => layout.ParentLayers != null && Enumerable.Contains(layout.ParentLayers, request.LayerName)))
		{
			EventDesignerRequest request2 = new EventDesignerRequest
			{
				ViewId = item.ViewId,
				Company = item.Company,
				TypeCode = item.TypeCode,
				LayerName = item.LayerName,
				DeviceType = item.DeviceType,
				CGCCode = item.CGCCode
			};
			Delete(request2, designerList);
			designerList.Remove(item);
		}
	}
}
