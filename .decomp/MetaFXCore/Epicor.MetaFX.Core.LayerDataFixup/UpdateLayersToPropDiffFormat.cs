using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.LayerDataFixup;

public class UpdateLayersToPropDiffFormat
{
	private readonly ViewManager viewManager;

	private readonly IContentLoader contentLoader;

	private readonly ILogger loggerForUpdateProcess;

	private readonly ILayerService layerService;

	private readonly string metaUIRootPath;

	private readonly SessionData currentSessionData;

	public UpdateLayersToPropDiffFormat(string metaUIRootPath, IContentLoader contentLoader, ILayerService layerService, ILogger loggerForUpdateProcess, ILogger loggerForLoadView, SessionData sessionData)
		: this(metaUIRootPath, contentLoader, layerService, loggerForUpdateProcess, loggerForLoadView, sessionData, null)
	{
	}

	internal UpdateLayersToPropDiffFormat(string metaUIRootPath, IContentLoader contentLoader, ILayerService layerService, ILogger loggerForUpdateProcess, ILogger loggerForLoadView, SessionData sessionData, ViewManager viewManager)
	{
		this.metaUIRootPath = metaUIRootPath;
		this.contentLoader = contentLoader;
		this.loggerForUpdateProcess = loggerForUpdateProcess;
		this.layerService = layerService;
		currentSessionData = sessionData;
		if (viewManager == null)
		{
			NoCacheService cacheService = new NoCacheService();
			this.viewManager = new ViewManager(metaUIRootPath, loggerForLoadView ?? this.loggerForUpdateProcess, layerService, cacheService, contentLoader);
		}
		else
		{
			this.viewManager = viewManager;
		}
	}

	public bool UpdateLayers([Optional] List<EpMetaFxLayerForConversion> layersToUpdate)
	{
		loggerForUpdateProcess.Debug("Processing " + (layersToUpdate?.Count.ToString() ?? "all") + " layers");
		return layerService.ProcessLayersForPropDiffUpdate(layersToUpdate, SetAllParentLayers, (EpMetaFxLayerForConversion layer) => ProcessLayer(layer, LoadViewWithLayer));
	}

	internal List<EpMetaFxLayerForConversion> SetAllParentLayers(List<EpMetaFxLayerForConversion> allLayers)
	{
		List<EpMetaFxLayerForConversion> list = allLayers.Where((EpMetaFxLayerForConversion l) => l.HasDraftConverted == false || l.HasPublishedConverted == false).ToList();
		loggerForUpdateProcess.Debug("Found " + (list.Any() ? list.Count.ToString() : "no") + " layers to process.");
		foreach (EpMetaFxLayerForConversion item in list)
		{
			loggerForUpdateProcess.Debug("Finding parent layers for current layer in process " + item.LayerName);
			item.AllParentLayers = CalculateAllParentLayers(allLayers, new List<EpMetaFxLayerForConversion>(), item);
		}
		list.Sort((EpMetaFxLayerForConversion lLayer, EpMetaFxLayerForConversion rLayer) => rLayer.AllParentLayers.Count.CompareTo(lLayer.AllParentLayers.Count));
		return list;
	}

	private List<EpMetaFxLayerForConversion> CalculateAllParentLayers(List<EpMetaFxLayerForConversion> allLayers, List<EpMetaFxLayerForConversion> allParentLayers, EpMetaFxLayerForConversion currentLayer)
	{
		string[] parentLayers = currentLayer.ParentLayers;
		if (parentLayers != null && parentLayers.Length != 0)
		{
			string[] parentLayers2 = currentLayer.ParentLayers;
			foreach (string p in parentLayers2)
			{
				if (p.Equals(currentLayer.LayerName, StringComparison.OrdinalIgnoreCase))
				{
					loggerForUpdateProcess.Debug(p + " parent layer is the same as current layer, ignore and continue");
					continue;
				}
				EpMetaFxLayerForConversion epMetaFxLayerForConversion = allLayers.FirstOrDefault((EpMetaFxLayerForConversion l) => l.LayerName.Equals(p, StringComparison.OrdinalIgnoreCase) && (l.Company == "" || l.Company.Equals(currentLayer.Company, StringComparison.OrdinalIgnoreCase)));
				if (epMetaFxLayerForConversion != null)
				{
					loggerForUpdateProcess.Debug("Found parent layer " + p + " for " + currentLayer.LayerName);
					allParentLayers.Add(epMetaFxLayerForConversion);
					CalculateAllParentLayers(allLayers, allParentLayers, epMetaFxLayerForConversion);
				}
			}
		}
		return allParentLayers;
	}

	internal bool ProcessLayer(EpMetaFxLayerForConversion layer, Action<EpMetaFxLayerForConversion, EpMetaFxRequest, bool> loadViewWithLayer)
	{
		loggerForUpdateProcess.Debug("Processing layer " + layer.LayerName + " of application " + layer.ViewId);
		EpMetaFxRequest arg = BuildMetaFxRequest(layer);
		try
		{
			if (layer.HasDraftConverted == false)
			{
				loggerForUpdateProcess.Debug("Processing draft content of the layer.");
				loadViewWithLayer(layer, arg, arg3: true);
			}
		}
		catch (Exception ex)
		{
			loggerForUpdateProcess.Debug("Failure while processing draft content of the layer. Error: " + ex.Message);
			return false;
		}
		try
		{
			if (layer.HasPublishedConverted == false)
			{
				loggerForUpdateProcess.Debug("Processing published content of the layer.");
				loadViewWithLayer(layer, arg, arg3: false);
			}
		}
		catch (Exception ex2)
		{
			loggerForUpdateProcess.Debug("Failure while processing published content of the layer. Error: " + ex2.Message);
			return false;
		}
		loggerForUpdateProcess.Debug("Layer processed successfully.");
		return true;
	}

	internal void LoadViewWithLayer(EpMetaFxLayerForConversion layer, EpMetaFxRequest epMetaFxViewRequest, bool isDraft)
	{
		currentSessionData.CompanyID = epMetaFxViewRequest.properties.company;
		currentSessionData.IsMultiTenant = false;
		layerService.WithNewSession(currentSessionData);
		viewManager.LoadViewsWithNoLock(epMetaFxViewRequest, () => string.Empty, (JObject strings, string programId) => (JObject)null, applyPersonalization: false, layer.TypeCode == "KNTCPersLayer", isDraft);
		if (isDraft)
		{
			layer.HasDraftConverted = true;
			layer.AllParentLayers?.ForEach(delegate(EpMetaFxLayerForConversion p)
			{
				if (p.HasDraftContent)
				{
					p.HasDraftConverted = true;
				}
				else
				{
					p.HasPublishedConverted = true;
				}
			});
		}
		else
		{
			layer.HasPublishedConverted = true;
			layer.AllParentLayers?.ForEach(delegate(EpMetaFxLayerForConversion p)
			{
				p.HasPublishedConverted = true;
			});
		}
	}

	internal static EpMetaFxRequest BuildMetaFxRequest(EpMetaFxLayerForConversion layer)
	{
		EpMetaFxRequest epMetaFxRequest = new EpMetaFxRequest();
		epMetaFxRequest.id = layer.ViewId;
		epMetaFxRequest.properties = new EpMetaFxRequestProperty
		{
			userName = ((layer.TypeCode == "KNTCPersLayer") ? layer.UserName : null),
			company = (layer.Company ?? string.Empty),
			deviceType = layer.DeviceType,
			layers = new string[1] { layer.LayerName },
			layerType = layer.TypeCode,
			basis = "ApplicationDiff",
			mode = "AppStudio"
		};
		return epMetaFxRequest;
	}
}
